namespace MoPubInternal.Editor.Postbuild
{
	using UnityEditor;
	using UnityEngine;
	using UnityEditor.Callbacks;
	using System.IO;
	using System.Collections.Generic;
	using System.Linq;
	using MoPubInternal.Editor.Postbuild;
	using MoPubInternal.Editor.ThirdParty.xcodeapi;

	public class MoPubPostBuildiOS : PostBuildiOS
	{
		private static string[] platformFrameworks = new string[] {
			"AdSupport.framework",
			"StoreKit.framework",
			"EventKit.framework",
			"EventKitUI.framework",
			"CoreTelephony.framework",
			"SafariServices.framework",
			// AdMob
			"MessageUI.framework",
			// Millennial
			"MediaPlayer.framework",
			"PassKit.framework",
			"Social.framework",
			"MobileCoreServices.framework",
			"WebKit.framework"
		};

		private static string[] frameworks = new string[] {
			"Fabric.framework"
		};

		private static string[] platformLibs = new string[] {
			"libz.dylib",
			"libsqlite3.dylib",
			"libxml2.dylib"
		};

		private static string[] libs = new string[] {
			"Fabric-Init/libFabriciOSInit.a",
			"MoPub/libMoPubSDK.a"
		};

		private static string nativeCodeInUnityPath = "Assets/MoPub/Editor/Support";
		private static string nativeCodeInXcodePath = "MoPub/NativeCode";

		[PostProcessBuild(100)]
		public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
		{
			// BuiltTarget.iOS is not defined in Unity 4, so we just use strings here
			if (buildTarget.ToString () == "iOS" || buildTarget.ToString () == "iPhone") {
				CheckiOSVersion ();

				PrepareProject (buildPath);
				PreparePlist (buildPath, "MoPub");

				RenameMRAIDSource (buildPath);
				InjectNativeCode (buildPath);
			}
		}

		private static void CheckiOSVersion()
		{
			iOSTargetOSVersion[] oldiOSVersions = {
				iOSTargetOSVersion.iOS_4_0,
				iOSTargetOSVersion.iOS_4_1,
				iOSTargetOSVersion.iOS_4_2,
				iOSTargetOSVersion.iOS_4_3,
				iOSTargetOSVersion.iOS_5_0,
				iOSTargetOSVersion.iOS_5_1,
				iOSTargetOSVersion.iOS_6_0
			};
			var isOldiOSVersion = oldiOSVersions.Contains (PlayerSettings.iOS.targetOSVersion);

			if (isOldiOSVersion) {
				Debug.LogWarning ("MoPub requires iOS 7+. Please change the Target iOS Version in Player Settings to iOS 7 or higher.");
			}
		}

		private static void PrepareProject(string buildPath)
		{
			string projPath = Path.Combine (buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
			PBXProject project = new PBXProject ();
			project.ReadFromString (File.ReadAllText(projPath));
			string target = project.TargetGuidByName ("Unity-iPhone");

			AddPlatformFrameworksToProject (platformFrameworks, project, target);
			AddFrameworksToProject (frameworks, buildPath, project, target);
			AddPlatformLibsToProject (platformLibs, project, target);
			AddLibsToProject (libs, project, target, buildPath);
			AddBuildProperty (project, target, "OTHER_LDFLAGS", "-ObjC");
			AddBuildProperty (project, target, "CLANG_ENABLE_MODULES", "YES");
			AddBuildProperty (project, target, "ENABLE_BITCODE", "NO");

			File.WriteAllText (projPath, project.WriteToString());
		}

		private static void RenameMRAIDSource (string buildPath)
		{
			// Unity will try to compile anything with the ".js" extension. Since mraid.js is not intended
			// for Unity, it'd break the build. So we store the file with a masked extension and after the
			// build rename it to the correct one.

			string[] maskedFiles = Directory.GetFiles (buildPath, "*.prevent_unity_compilation", SearchOption.AllDirectories);
			foreach (string maskedFile in maskedFiles) {
				string unmaskedFile = maskedFile.Replace (".prevent_unity_compilation", "");
				File.Move(maskedFile, unmaskedFile);
			}
		}

		private static void InjectNativeCode (string buildPath)
		{
			string projPath = Path.Combine (buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
			string nativeCodeInUnityFullPath = Path.Combine (Directory.GetCurrentDirectory (), nativeCodeInUnityPath);
			string nativeCodeInXcodeFullPath = Path.Combine (buildPath, nativeCodeInXcodePath);

			PBXProject project = new PBXProject();
			project.ReadFromString(File.ReadAllText(projPath));

			DirectoryCopy (nativeCodeInUnityFullPath, nativeCodeInXcodeFullPath, true);

			string targetGuid = project.TargetGuidByName ("Unity-iPhone");
			string[] files = Directory.GetFiles (nativeCodeInXcodeFullPath, "*", SearchOption.AllDirectories);

			foreach (string fileFullPath in files) {
				string fileExt = Path.GetExtension (fileFullPath);
				if (fileExt == ".meta" || fileExt == ".txt") {
					continue;
				}

				string fileName = Path.GetFileName (fileFullPath);
				string fileGuid = project.AddFile (fileFullPath, Path.Combine (nativeCodeInXcodePath, fileName));
				project.AddFileToBuild (targetGuid, fileGuid);
			}

			string[] subdirs = Directory.GetDirectories (nativeCodeInXcodeFullPath);
			foreach (string subdir in subdirs) {
				project.AddBuildProperty (targetGuid, "HEADER_SEARCH_PATHS", subdir);

				AddAllFoundFrameworks(project, targetGuid, subdir);
			}

			File.WriteAllText(projPath, project.WriteToString());
		}

		private static void AddAllFoundFrameworks(PBXProject project, string targetGuid, string source)
		{
			string[] dirs = Directory.GetDirectories (source);
			foreach (string dir in dirs) {
				if (dir.EndsWith (".framework")) {
					string fileGuid = project.AddFile (dir, "Frameworks/" + dir.Substring (dir.LastIndexOf ("/") + 1));
					project.AddFileToBuild (targetGuid, fileGuid);
					project.AddBuildProperty (targetGuid, "FRAMEWORK_SEARCH_PATHS", source);
				}

				AddAllFoundFrameworks (project, targetGuid, dir);
			}
		}
	}
}
