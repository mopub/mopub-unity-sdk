#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#if UNITY_2017_1_OR_NEWER
using UnityEditor.iOS.Xcode.Extensions;
#endif

using UnityEngine;

// ReSharper disable once CheckNamespace
namespace MoPubInternal.Editor.Postbuild
{
    public static class MoPubPostBuildiOS
    {
        private static readonly string[] PlatformLibs = { "libz.dylib", "libsqlite3.dylib", "libxml2.dylib" };

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            // BuiltTarget.iOS is not defined in Unity 4, so we just use strings here
            if (buildTarget.ToString() != "iOS" && buildTarget.ToString() != "iPhone") return;
            CheckiOSVersion();
            PrepareProject(buildPath);
            RemoveMetaFiles(buildPath);
            RenameMRAIDSource(buildPath);
        }

        private static void CheckiOSVersion()
        {
            var iOSTargetVersion = double.Parse(PlayerSettings.iOS.targetOSVersionString, System.Globalization.CultureInfo.InvariantCulture);
            if (iOSTargetVersion < 7) {
                Debug.LogWarning("MoPub requires iOS 7+. Please change the Target iOS Version in Player Settings to " +
                                 "iOS 7 or higher.");
            }
        }

        private static void PrepareProject(string buildPath)
        {
            var projPath = Path.Combine(buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projPath));
            var target = project.TargetGuidByName("Unity-iPhone");

            foreach (var lib in PlatformLibs) {
                string libGUID = project.AddFile("usr/lib/" + lib, "Libraries/" + lib, PBXSourceTree.Sdk);
                project.AddFileToBuild(target, libGUID);
            }

            bool emitWarning = true;
#if UNITY_2017_1_OR_NEWER
            var fileGuid = project.FindFileGuidByProjectPath("Frameworks/MoPub/Plugins/iOS/MoPubSDKFramework.framework")
                        // Unity 2018.3 leaves out the intermediate directories.
                        ?? project.FindFileGuidByProjectPath("Frameworks/MoPubSDKFramework.framework")
                        // Check legacy location in case post 5.4 file migration has not been done.
                        ?? project.FindFileGuidByProjectPath("Frameworks/Plugins/iOS/MoPubSDKFramework.framework");
            if (fileGuid != null) {
                project.AddFileToEmbedFrameworks(target, fileGuid);
                emitWarning = false;
            }
#endif
            if (emitWarning)
                Debug.LogWarning(
                    "Unable to automatically add MoPubSDKFramework.framework to the Embedded Binaries list in the Xcode project.\n" +
                    "Please add it manually in Xcode, under the General properties of the Unity-iPhone target, unless you\n" +
                    "are building against the SDK source (via Cocoapods) or static library.");

            project.SetBuildProperty(
                target, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");

            project.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
            project.AddBuildProperty(target, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
            project.AddBuildProperty(target, "CLANG_ENABLE_MODULES", "YES");
            project.AddBuildProperty(target, "ENABLE_BITCODE", "NO");

            File.WriteAllText(projPath, project.WriteToString());
        }

        private static void RemoveMetaFiles(string buildPath)
        {
            // Remove all the .meta files that Unity copies into the Xcode subdirectories.
            foreach (var subdir in new[] { "Frameworks/MoPub/Plugins/iOS", "Libraries/MoPub/Plugins/iOS",
                                           // Legacy locations...
                                           "Frameworks/Plugins/iOS", "Libraries/Plugins/iOS" }) {
                var path = Path.Combine(buildPath, subdir);
                if (!Directory.Exists(path))
                    continue;
                var metaFiles = Directory.GetFiles(path, "*.meta", SearchOption.AllDirectories);
                foreach (var metaFile in metaFiles) {
                    File.Delete(metaFile);
                }
            }
        }

        private static void RenameMRAIDSource(string buildPath)
        {
            // Unity will try to compile anything with the ".js" extension. Since mraid.js is not intended
            // for Unity, it'd break the build. So we store the file with a masked extension and after the
            // build rename it to the correct one.

            var maskedFiles = Directory.GetFiles(
                buildPath, "*.prevent_unity_compilation", SearchOption.AllDirectories);
            foreach (var maskedFile in maskedFiles) {
                var unmaskedFile = maskedFile.Replace(".prevent_unity_compilation", "");
                File.Move(maskedFile, unmaskedFile);
            }
        }
    }
}
#endif
