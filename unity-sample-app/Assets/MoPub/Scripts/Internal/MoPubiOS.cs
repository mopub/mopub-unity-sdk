using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Bridge between the MoPub Unity Instance-wide API and iOS implementation.
/// </summary>
/// <para>
/// Publishers integrating with MoPub should make all calls through the <see cref="MoPub"/> class, and handle any
/// desired MoPub Events from the <see cref="MoPubManager"/> class.
/// </para>
/// <para>
/// For other platform-specific implementations, see <see cref="MoPubUnityEditor"/> and <see cref="MoPubAndroid"/>.
/// </para>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal class MoPubiOS : MoPubPlatformApi
{
    #region SdkSetup


    internal override void InitializeSdk(MoPub.SdkConfiguration sdkConfiguration)
    {
        _moPubInitializeSdk(sdkConfiguration.AdUnitId, sdkConfiguration.AdditionalNetworksString,
                            sdkConfiguration.MediationSettingsJson, sdkConfiguration.AllowLegitimateInterest,
                            (int) sdkConfiguration.LogLevel, sdkConfiguration.NetworkConfigurationsJson,
                            sdkConfiguration.MoPubRequestOptionsJson);
    }


    internal override void EnableLocationSupport(bool shouldUseLocation)
    {
        _moPubEnableLocationSupport(shouldUseLocation);
    }


    internal override void SetEngineInformation(string engineName, string engineVersion)
    {
        _moPubSetEngineInformation(engineName, engineVersion);
    }


    internal override void ReportApplicationOpen(string iTunesAppId = null)
    {
        _moPubReportApplicationOpen(iTunesAppId);
    }


    internal override void OnApplicationPause(bool paused)
    {
        MoPubManager.EmitConsentDialogDismissedIfApplicable(paused);
    }


    internal override bool AllowLegitimateInterest
    {
        get { return _moPubAllowLegitimateInterest(); }
        set { _moPubSetAllowLegitimateInterest(value); }
    }


    internal override string SdkName
    {
        get { return "iOS SDK v" + _moPubGetSDKVersion(); }
    }


    internal override bool IsSdkInitialized
    {
        get { return _moPubIsSdkInitialized(); }
    }


    internal override MoPub.LogLevel SdkLogLevel
    {
        get { return (MoPub.LogLevel) _moPubGetLogLevel(); }
        set { _moPubSetLogLevel((int) value); }
    }


    #endregion SdkSetup


    #region AndroidOnly


    internal override void AddFacebookTestDeviceId(string hashedDeviceId)
    {
        Debug.LogWarning("Attempted to call Android-only method from iOS device.");
    }


    #endregion AndroidOnly


    #region iOSOnly


    internal override void ForceWKWebView(bool shouldForce)
    {
        _moPubForceWKWebView(shouldForce);
    }


    #endregion iOSOnly


    #region UserConsent

    internal override bool CanCollectPersonalInfo
    {
        get { return _moPubCanCollectPersonalInfo(); }
        set { }
    }


    internal override MoPub.Consent.Status CurrentConsentStatus
    {
        get { return (MoPub.Consent.Status) _moPubCurrentConsentStatus(); }
        set { }
    }


    internal override bool ShouldShowConsentDialog
    {
        get { return _moPubShouldShowConsentDialog(); }
    }


    internal override void LoadConsentDialog()
    {
        _moPubLoadConsentDialog();
    }


    internal override bool IsConsentDialogReady
    {
        get { return _moPubIsConsentDialogReady(); }
        set { }
    }


    internal override void ShowConsentDialog()
    {
        _moPubShowConsentDialog();
    }


    internal override bool? IsGdprApplicable {
        get {
            var value = _moPubIsGDPRApplicable();
            return value == 0 ? null : value > 0 ? (bool?) true : false;
        }
    }


    internal override void ForceGdprApplicable() {
        _moPubForceGDPRApplicable();
    }


    internal override void GrantConsent()
    {
        _moPubGrantConsent();
    }


    internal override void RevokeConsent()
    {
        _moPubRevokeConsent();
    }


    internal override string CurrentConsentPrivacyPolicyUrl
    {
        get {
            return _moPubCurrentConsentPrivacyPolicyUrl(MoPub.ConsentLanguageCode);
        }
        set { }
    }


    internal override string CurrentVendorListUrl
    {
        get {
            return _moPubCurrentConsentVendorListUrl(MoPub.ConsentLanguageCode);
        }
        set { }
    }


    internal override string CurrentConsentIabVendorListFormat
    {
        get { return _moPubCurrentConsentIabVendorListFormat(); }
        set { }
    }


    internal override string CurrentConsentPrivacyPolicyVersion
    {
        get { return _moPubCurrentConsentPrivacyPolicyVersion(); }
        set { }
    }


    internal override string CurrentConsentVendorListVersion
    {
        get { return _moPubCurrentConsentVendorListVersion(); }
        set { }
    }


    internal override string PreviouslyConsentedIabVendorListFormat
    {
        get { return _moPubPreviouslyConsentedIabVendorListFormat(); }
        set { }
    }


    internal override string PreviouslyConsentedPrivacyPolicyVersion
    {
        get { return _moPubPreviouslyConsentedPrivacyPolicyVersion(); }
        set { }
    }


    internal override string PreviouslyConsentedVendorListVersion
    {
        get { return _moPubPreviouslyConsentedVendorListVersion(); }
        set { }
    }

    #endregion UserConsent


    #region DllImports
#if ENABLE_IL2CPP && UNITY_ANDROID
    // IL2CPP on Android scrubs DllImports, so we need to provide stubs to unblock compilation
    private static void _moPubInitializeSdk(string adUnitId, string additionalNetworksJson,
                                            string mediationSettingsJson, bool allowLegitimateInterest,
                                            int logLevel, string adapterConfigJson,
                                            string moPubRequestOptionsJson) {}
    private static bool _moPubIsSdkInitialized() { return false; }
    private static string _moPubGetSDKVersion() { return null; }
    private static void _moPubEnableLocationSupport(bool shouldUseLocation) {}
    private static void _moPubSetEngineInformation(string name, string version) {}
    private static void _moPubSetAllowLegitimateInterest(bool allowLegitimateInterest) {}
    private static bool _moPubAllowLegitimateInterest() { return false; }
    private static int _moPubGetLogLevel() { return -1; }
    private static void _moPubSetLogLevel(int logLevel) {}
    private static void _moPubForceWKWebView(bool shouldForce) {}
    private static void _moPubReportApplicationOpen(string iTunesAppId) {}
    private static bool _moPubCanCollectPersonalInfo() { return false; }
    private static int _moPubCurrentConsentStatus() { return -1; }
    private static int _moPubIsGDPRApplicable() { return -1; }
    private static int _moPubForceGDPRApplicable() { return -1; }
    private static bool _moPubShouldShowConsentDialog() { return false; }
    private static bool _moPubIsConsentDialogReady() { return false; }
    private static void _moPubLoadConsentDialog() {}
    private static void _moPubShowConsentDialog() {}
    private static string _moPubCurrentConsentPrivacyPolicyUrl(string isoLanguageCode = null) { return null; }
    private static string _moPubCurrentConsentVendorListUrl(string isoLanguageCode = null) { return null; }
    private static void _moPubGrantConsent() {}
    private static void _moPubRevokeConsent() {}
    private static string _moPubCurrentConsentIabVendorListFormat() { return null; }
    private static string _moPubCurrentConsentPrivacyPolicyVersion() { return null; }
    private static string _moPubCurrentConsentVendorListVersion() { return null; }
    private static string _moPubPreviouslyConsentedIabVendorListFormat() { return null; }
    private static string _moPubPreviouslyConsentedPrivacyPolicyVersion() { return null; }
    private static string _moPubPreviouslyConsentedVendorListVersion() { return null; }
#else
    [DllImport("__Internal")]
    private static extern void _moPubInitializeSdk(string adUnitId, string additionalNetworksJson,
                                                   string mediationSettingsJson, bool allowLegitimateInterest,
                                                   int logLevel, string adapterConfigJson,
                                                   string moPubRequestOptionsJson);


    [DllImport("__Internal")]
    private static extern bool _moPubIsSdkInitialized();


    [DllImport("__Internal")]
    private static extern string _moPubGetSDKVersion();


    [DllImport("__Internal")]
    private static extern void _moPubEnableLocationSupport(bool shouldUseLocation);


    [DllImport("__Internal")]
    private static extern void _moPubSetEngineInformation(string name, string version);


    [DllImport("__Internal")]
    private static extern void _moPubSetAllowLegitimateInterest(bool allowLegitimateInterest);


    [DllImport("__Internal")]
    private static extern bool _moPubAllowLegitimateInterest();

    [DllImport("__Internal")]
    private static extern int _moPubGetLogLevel();


    [DllImport("__Internal")]
    private static extern void _moPubSetLogLevel(int logLevel);


    [DllImport("__Internal")]
    private static extern void _moPubForceWKWebView(bool shouldForce);


    [DllImport("__Internal")]
    private static extern void _moPubReportApplicationOpen(string iTunesAppId);


    [DllImport("__Internal")]
    private static extern bool _moPubCanCollectPersonalInfo();


    [DllImport("__Internal")]
    private static extern int _moPubCurrentConsentStatus();


    [DllImport("__Internal")]
    private static extern int _moPubIsGDPRApplicable();


    [DllImport("__Internal")]
    private static extern int _moPubForceGDPRApplicable();


    [DllImport("__Internal")]
    private static extern bool _moPubShouldShowConsentDialog();


    [DllImport("__Internal")]
    private static extern bool _moPubIsConsentDialogReady();


    [DllImport("__Internal")]
    private static extern void _moPubLoadConsentDialog();


    [DllImport("__Internal")]
    private static extern void _moPubShowConsentDialog();


    [DllImport("__Internal")]
    private static extern string _moPubCurrentConsentPrivacyPolicyUrl(string isoLanguageCode = null);


    [DllImport("__Internal")]
    private static extern string _moPubCurrentConsentVendorListUrl(string isoLanguageCode = null);


    [DllImport("__Internal")]
    private static extern void _moPubGrantConsent();


    [DllImport("__Internal")]
    private static extern void _moPubRevokeConsent();


    [DllImport("__Internal")]
    private static extern string _moPubCurrentConsentIabVendorListFormat();


    [DllImport("__Internal")]
    private static extern string _moPubCurrentConsentPrivacyPolicyVersion();


    [DllImport("__Internal")]
    private static extern string _moPubCurrentConsentVendorListVersion();


    [DllImport("__Internal")]
    private static extern string _moPubPreviouslyConsentedIabVendorListFormat();


    [DllImport("__Internal")]
    private static extern string _moPubPreviouslyConsentedPrivacyPolicyVersion();


    [DllImport("__Internal")]
    private static extern string _moPubPreviouslyConsentedVendorListVersion();
#endif
    #endregion DllImports
}
