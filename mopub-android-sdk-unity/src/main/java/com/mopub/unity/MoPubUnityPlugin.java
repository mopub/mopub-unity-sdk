package com.mopub.unity;

import android.app.Activity;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.text.TextUtils;
import android.util.Log;

import com.fasterxml.jackson.jr.ob.JSON;
import com.fasterxml.jackson.jr.private_.TreeNode;
import com.fasterxml.jackson.jr.stree.JacksonJrsTreeCodec;
import com.mopub.common.MediationSettings;
import com.mopub.common.MoPub;
import com.mopub.common.SdkConfiguration;
import com.mopub.common.SdkInitializationListener;
import com.mopub.common.logging.MoPubLog;
import com.mopub.common.privacy.ConsentData;
import com.mopub.common.privacy.ConsentDialogListener;
import com.mopub.common.privacy.ConsentStatus;
import com.mopub.common.privacy.ConsentStatusChangeListener;
import com.mopub.common.privacy.PersonalInfoManager;
import com.mopub.mobileads.MoPubConversionTracker;
import com.mopub.mobileads.MoPubErrorCode;
import com.mopub.network.ImpressionData;
import com.mopub.network.ImpressionListener;
import com.mopub.network.ImpressionsEmitter;
import com.unity3d.player.UnityPlayer;

import java.io.IOException;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;


/**
 * Base class for every available ad format plugin. Exposes APIs that the plugins might need.
 * Operations and properties that apply to the SDK as a whole are here as static methods.
 */
public class MoPubUnityPlugin {
    protected static String TAG = "MoPub";


    protected enum UnityEvent {
        // Init
        SdkInitialized("SdkInitialized"),
        // Consent
        ConsentDialogLoaded("ConsentDialogLoaded"),
        ConsentDialogFailed("ConsentDialogFailed"),
        ConsentDialogShown("ConsentDialogShown"),
        ConsentStatusChanged("ConsentStatusChanged"),
        // Banners
        AdLoaded("AdLoaded"),
        AdFailed("AdFailed"),
        AdClicked("AdClicked"),
        AdExpanded("AdExpanded"),
        AdCollapsed("AdCollapsed"),
        // Interstitials
        InterstitialLoaded("InterstitialLoaded"),
        InterstitialExpired("InterstitialExpired"),
        InterstitialFailed("InterstitialFailed"),
        InterstitialShown("InterstitialShown"),
        InterstitialClicked("InterstitialClicked"),
        InterstitialDismissed("InterstitialDismissed"),
        // Rewarded Videos
        RewardedVideoLoaded("RewardedVideoLoaded"),
        RewardedVideoExpired("RewardedVideoExpired"),
        RewardedVideoFailed("RewardedVideoFailed"),
        RewardedVideoShown("RewardedVideoShown"),
        RewardedVideoClicked("RewardedVideoClicked"),
        RewardedVideoFailedToPlay("RewardedVideoFailedToPlay"),
        RewardedVideoClosed("RewardedVideoClosed"),
        RewardedVideoReceivedReward("RewardedVideoReceivedReward"),
        // Native Ads
        NativeImpression("NativeImpression"),
        NativeClick("NativeClick"),
        NativeLoad("NativeLoad"),
        NativeFail("NativeFail"),
        // Impressions
        ImpressionTracked("ImpressionTracked");

        @NonNull final private String name;

        UnityEvent(@NonNull final String name) { this.name = "Emit" + name + "Event"; }

        public void Emit(String... args) {
            try {
                UnityPlayer.UnitySendMessage("MoPubManager", name, JSON.std.asString(args));
            } catch (IOException e) {
                Log.e(TAG, "Exception sending message to Unity", e);
            };
        }
    }


    private static final ConsentStatusChangeListener consentListener =
        new ConsentStatusChangeListener() {
            @Override
            public void onConsentStateChange(@NonNull ConsentStatus oldConsentStatus,
                                             @NonNull ConsentStatus newConsentStatus,
                                             boolean canCollectPersonalInformation) {
                UnityEvent.ConsentStatusChanged.Emit(
                    oldConsentStatus.getValue(), newConsentStatus.getValue(),
                        canCollectPersonalInformation ? "true" : "false"
                );
            }
        };


    /**
     * The ad unit that this plugin class manages the content for.
     */
    protected final String mAdUnitId;


    /**
     * Subclasses should use this to create instances of themselves.
     *
     * @param adUnitId String for the ad unit ID to use for the plugin.
     */
    public MoPubUnityPlugin(final String adUnitId) {
        mAdUnitId = adUnitId;
    }


    /**
     * Initializes the MoPub SDK. Call this before making any rewarded ads or advanced bidding
     * requests. This will do the rewarded video custom event initialization any number of times,
     * but the SDK itself can only be initialized once, and the rewarded ads module can only be
     * initialized once.
     *
     * @param adUnitId String with any ad unit id used by this app.
     * @param adapterConfigurationClassesString String of comma-separated custom adapter
     *                                          configuration classes.
     * @param mediationSettingsJson String with JSON containing third-party network specific
     *                              settings.
     * @param allowLegitimateInterest Flag to allow networks to gather user data.
     * @param logLevel The log level enum name.
     * @param mediatedNetworkConfigurationsJson String with JSON containing adapter configuration
     *                                          options used to initialize third-party networks.
     * @param moPubRequestOptionsJson String with JSON containing adapter configuration options
     */
    public static void initializeSdk(final String adUnitId,
                                     final String adapterConfigurationClassesString,
                                     final String mediationSettingsJson,
                                     final boolean allowLegitimateInterest,
                                     final int logLevel,
                                     final String mediatedNetworkConfigurationsJson,
                                     final String moPubRequestOptionsJson) {

        final SdkConfiguration.Builder sdkConfigurationBuilder =
            new SdkConfiguration.Builder(adUnitId)
                                .withLegitimateInterestAllowed(allowLegitimateInterest)
                                .withLogLevel(MoPubLog.LogLevel.valueOf(logLevel));

        if (!TextUtils.isEmpty(adapterConfigurationClassesString))
            for (String adapterConfigClass : adapterConfigurationClassesString.split("\\s*,\\s*"))
                sdkConfigurationBuilder.withAdditionalNetwork(adapterConfigClass.trim());

        final Map<String, Map<String, String>> mediatedNetworkConfigurations =
                extractOptionsMapFromJson(mediatedNetworkConfigurationsJson);
        if (mediatedNetworkConfigurations != null)
            for (String adapterConfigClass : mediatedNetworkConfigurations.keySet())
                sdkConfigurationBuilder.withMediatedNetworkConfiguration(adapterConfigClass,
                        mediatedNetworkConfigurations.get(adapterConfigClass));

        final MediationSettings[] mediationSettings =
                extractMediationSettingsFromJson(mediationSettingsJson);
        if (mediationSettings != null && mediationSettings.length > 0)
            sdkConfigurationBuilder.withMediationSettings(mediationSettings);

        final Map<String, Map<String, String>> moPubRequestOptions =
                extractOptionsMapFromJson(moPubRequestOptionsJson);
        if (moPubRequestOptions != null)
            for (String adapterConfigClass : moPubRequestOptions.keySet())
                sdkConfigurationBuilder.withMediatedNetworkConfiguration(adapterConfigClass,
                        moPubRequestOptions.get(adapterConfigClass));

        final SdkInitializationListener initListener = new SdkInitializationListener() {
            @Override
            public void onInitializationFinished() {
                UnityEvent.SdkInitialized.Emit(adUnitId, Integer.toString(logLevel));
            }
        };

        runSafelyOnUiThread(new Runnable() {
            public void run() {
                MoPub.initializeSdk(getActivity(), sdkConfigurationBuilder.build(), initListener);
                // Note: Subscribing to the Consent Status Change is valid as soon as initializeSdk() returns,
                // as that is when the PersonalInformationManager property gets initialized. Thus, keeping here instead
                // of in onInitializationFinished to avoid race conditions.
                PersonalInfoManager piiManager = MoPub.getPersonalInformationManager();
                if (piiManager != null) {
                    piiManager.subscribeConsentStatusChangeListener(consentListener);
                }
            }
        });

        ImpressionsEmitter.addListener(new ImpressionListener() {
            @Override
            public void onImpression(@NonNull final String adUnitId,
                                     @Nullable final ImpressionData impressionData) {
                if (impressionData != null) {
                    UnityEvent.ImpressionTracked.Emit(adUnitId,
                            impressionData.getJsonRepresentation().toString());
                } else {
                    UnityEvent.ImpressionTracked.Emit(adUnitId);
                }
            }
        });
    }


    public static boolean isSdkInitialized() { return MoPub.isSdkInitialized(); }


    public static boolean shouldAllowLegitimateInterest() {
        return MoPub.shouldAllowLegitimateInterest();
    }

    public static void setAllowLegitimateInterest(final boolean allowLegitimateInterest) {
        MoPub.setAllowLegitimateInterest(allowLegitimateInterest);
    }


    public static int getLogLevel() {
        return MoPubLog.getLogLevel().intValue();
    }

    public static void setLogLevel(final int logLevel) {
        MoPubLog.setLogLevel(MoPubLog.LogLevel.valueOf(logLevel));
    }


    /**
     * Registers the given device as a Facebook Ads test device.
     * See https://developers.facebook.com/docs/reference/android/current/class/AdSettings/
     *
     * @param hashedDeviceId String with the hashed ID of the device.
     */
    public static void addFacebookTestDeviceId(String hashedDeviceId) {
        try {
            Class<?> cls = Class.forName("com.facebook.ads.AdSettings");
            Method method = cls.getMethod("addTestDevice", String.class);
            method.invoke(cls, hashedDeviceId);
            Log.i(TAG, "successfully added Facebook test device: " + hashedDeviceId);
        } catch (ClassNotFoundException e) {
            Log.i(TAG, "could not find Facebook AdSettings class. " +
                    "Did you add the Audience Network SDK to your Android folder?");
        } catch (NoSuchMethodException e) {
            Log.i(TAG, "could not find Facebook AdSettings.addTestDevice method. " +
                    "Did you add the Audience Network SDK to your Android folder?");
        } catch (IllegalAccessException e) {
            Log.e(TAG, "Exception while adding Facebook test device id", e);
        } catch (InvocationTargetException e) {
            Log.e(TAG, "Exception while adding Facebook test device id", e);
        }
    }


    /**
     * Reports an application being open for conversion tracking purposes.
     */
    public static void reportApplicationOpen() {
        runSafelyOnUiThread(new Runnable() {
            public void run() {
                new MoPubConversionTracker(getActivity()).reportAppOpen();
            }
        });
    }


    /**
     * Inform the SDK of a change in the app's pause status.
     *
     * @param paused True if pausing, false if resuming.
     */
    public static void onApplicationPause(final boolean paused) {
        if (!MoPub.isSdkInitialized())  // If not initialized, there are no listeners to notify.
            return;
        if (paused)
            MoPub.onPause(getActivity());
        else
            MoPub.onResume(getActivity());
    }

    /**
     * Specifies the desired location awareness settings: DISABLED, TRUNCATED or NORMAL.
     *
     * @param locationAwareness String with location awareness setting to be parsed as a
     *      {@link com.mopub.common.MoPub.LocationAwareness}.
     */
    public static void setLocationAwareness(final String locationAwareness) {
        runSafelyOnUiThread(new Runnable() {
            public void run() {
                MoPub.setLocationAwareness(MoPub.LocationAwareness.valueOf(locationAwareness));
            }
        });
    }


    /* ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** *****
     * Consent Methods                                                                          *
     * ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** *****/

    /**
     * Whether or not the SDK is allowed to collect personally identifiable information.
     *
     * @return true if able to collect personally identifiable information.
     */
    public static boolean canCollectPersonalInfo() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        return pim != null && pim.canCollectPersonalInformation();
    }


    /**
     * The user's current consent state.
     *
     * @return The string value of the ConsentStatus representing the current consent status.
     */
    @Nullable
    public static String getPersonalInfoConsentState() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        return pim != null ? pim.getPersonalInfoConsentStatus().getValue() : null;
    }


    /**
     * Returns whether or not the SDK thinks the user is in a GDPR region or not. Returns true for
     * in a GDPR region, no for not in a GDPR region, and null for unknown.
     *
     * @return +1 for in GDPR region, -1 for not in GDPR region, 0 for unknown
     */
    public static int gdprApplies() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        Boolean applies = pim != null ? pim.gdprApplies() : null;
        return applies == null ? 0 : applies ? 1 : -1;
    }

    /**
     * Forces the SDK to treat this app as in a GDPR region. Setting this will permanently force
     * GDPR rules for this user unless this app is uninstalled or the data for this app is cleared.
     */
    public static void forceGdprApplies() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        if (pim == null) {
            Log.e(TAG, "Failed to force GDPR applicability; did you initialize the MoPub " +
                    "SDK?");
        } else {
            pim.forceGdprApplies();
        }
    }

    /**
     * Checks to see if a publisher should load and then show a consent dialog.
     *
     * @return True for yes, false for no.
     */
    public static boolean shouldShowConsentDialog() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        return pim != null && pim.shouldShowConsentDialog();
    }


    /**
     * Whether or not the consent dialog is done loading and ready to show.
     *
     * @return True for yes, false for no.
     */
    public static boolean isConsentDialogReady() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        return pim != null && pim.isConsentDialogReady();
    }


    /**
     * Sends off a request to load the MoPub consent dialog.
     */
    public static void loadConsentDialog() {
        runSafelyOnUiThread(new Runnable() {
            @Override
            public void run() {
                PersonalInfoManager pim = MoPub.getPersonalInformationManager();
                if (pim != null) pim.loadConsentDialog(new ConsentDialogListener() {
                    @Override
                    public void onConsentDialogLoaded() {
                        UnityEvent.ConsentDialogLoaded.Emit();
                    }

                    @Override
                    public void onConsentDialogLoadFailed(@NonNull MoPubErrorCode moPubErrorCode) {
                        UnityEvent.ConsentDialogFailed.Emit(moPubErrorCode.toString());
                    }
                });
            }
        });
    }


    /**
     * If the MoPub consent dialog is loaded, then show it.
     */
    public static void showConsentDialog() {
        runSafelyOnUiThread(new Runnable() {
            @Override
            public void run() {
                PersonalInfoManager pim = MoPub.getPersonalInformationManager();
                if (pim != null && pim.showConsentDialog()) {
                    UnityEvent.ConsentDialogShown.Emit();
                }
            }
        });
    }


    @Nullable
    public static String getCurrentPrivacyPolicyLink(String isoLanguageCode) {
        return getNullableConsentData().getCurrentPrivacyPolicyLink(isoLanguageCode);
    }


    @Nullable
    public static String getCurrentVendorListLink(String isoLanguageCode) {
        return getNullableConsentData().getCurrentVendorListLink(isoLanguageCode);
    }


    /**
     * For use from whitelisted publishers only. Grants consent to collect personally identifiable
     * information for the current user.
     */
    public static void grantConsent() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        if (pim != null) pim.grantConsent();
    }


    /**
     * For use from whitelisted publishers only. Denies consent to collect personally identifiable
     * information for the current user.
     */
    public static void revokeConsent() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        if (pim != null) pim.revokeConsent();
    }


    @Nullable
    public String getCurrentVendorListVersion() {
        return getNullableConsentData().getCurrentVendorListVersion();
    }


    @Nullable
    public String getCurrentPrivacyPolicyVersion() {
        return getNullableConsentData().getCurrentPrivacyPolicyVersion();
    }


    @Nullable
    public String getCurrentVendorListIabFormat() {
        return getNullableConsentData().getCurrentVendorListIabFormat();
    }


    @Nullable
    public String getConsentedPrivacyPolicyVersion() {
        return getNullableConsentData().getConsentedPrivacyPolicyVersion();
    }


    @Nullable
    public String getConsentedVendorListVersion() {
        return getNullableConsentData().getConsentedVendorListVersion();
    }


    @Nullable
    public String getConsentedVendorListIabFormat() {
        return getNullableConsentData().getConsentedVendorListIabFormat();
    }



    /* ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** *****
     * Helper Methods                                                                          *
     * ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** ***** *****/

    public static String getSDKVersion() {
        return MoPub.SDK_VERSION;
    }

    protected static Activity getActivity() {
        return UnityPlayer.currentActivity;
    }

    protected static void runSafelyOnUiThread(final Runnable runner) {
        getActivity().runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    runner.run();
                } catch (Exception e) {
                    Log.e(TAG, "Exception running task on UI thread", e);
                }
            }
        });
    }

    protected static MediationSettings[] extractMediationSettingsFromJson(final String json) {
        if (TextUtils.isEmpty(json))
            return null;

        final JSON jsonReader = JSON.std.with(new JacksonJrsTreeCodec());

        TreeNode jsonTree;
        try {
            jsonTree = jsonReader.treeFrom(json);
        } catch (IOException e) {
            Log.e(TAG, "Exception while reading mediation settings", e);
            return null;
        }
        if (jsonTree == null || !jsonTree.isObject()) {
            Log.e(TAG, "Expected a JSON object for mediation settings");
            return null;
        }

        final ArrayList<MediationSettings> settings = new ArrayList<>();
        for (Iterator<String> keys = jsonTree.fieldNames(); keys.hasNext(); )
            try {
                final Class<?> mediationSettingsClass = Class.forName(keys.next());
                final TreeNode mediationSettingsData = jsonTree.get(mediationSettingsClass.getName());
                if (mediationSettingsData != null && mediationSettingsData.isObject()) {
                    Log.i(TAG, "Adding mediation settings " + mediationSettingsClass);
                    final Object o = jsonReader.beanFrom(mediationSettingsClass, mediationSettingsData.traverse());
                    if (o != null)
                        settings.add((MediationSettings) o);
                } else {
                    Log.e(TAG, "Expected a JSON object for mediation settings key " + mediationSettingsClass);
                }
            } catch (Exception e) {
                Log.e(TAG, "Exception while reading mediation settings", e);
            }

        return settings.toArray(new MediationSettings[0]);
    }

    protected static Map<String, Map<String, String>> extractOptionsMapFromJson(final String json)
    {
        if (TextUtils.isEmpty(json))
            return null;

        final JSON jsonReader = JSON.std.with(new JacksonJrsTreeCodec());

        TreeNode jsonTree;
        try {
            jsonTree = jsonReader.treeFrom(json);
        } catch (IOException e) {
            Log.e(TAG, "Exception while reading options map", e);
            return null;
        }
        if (jsonTree == null || !jsonTree.isObject()) {
            Log.e(TAG, "Expected a JSON object for options map");
            return null;
        }

        final Map<String, Map<String, String>> allOptions = new HashMap<>();
        for (Iterator<String> adapterConfigClasses = jsonTree.fieldNames(); adapterConfigClasses.hasNext(); ) {
            final String adapterConfigClass = adapterConfigClasses.next();
            final TreeNode optionsData = jsonTree.get(adapterConfigClass);
            if (optionsData != null && optionsData.isObject()) {
                final Map<String, String> options = new HashMap<>();
                for (Iterator<String> keys2 = optionsData.fieldNames(); keys2.hasNext(); ) {
                    String key = keys2.next();
                    
                    TreeNode fieldNode = optionsData.get(key);
                    if (fieldNode.isValueNode()) {
                        options.put(key, ((JrsValue)fieldNode).asText());
                    } else {
                        options.put(key, fieldNode.toString());
                    }
                }
                allOptions.put(adapterConfigClass, options);
            } else {
                Log.e(TAG, "Expected a JSON object for adapter configuration options");
            }
        }

        return allOptions;
    }

    private static final ConsentData NULL_CONSENT_DATA = new NullConsentData();

    @NonNull
    private static ConsentData getNullableConsentData() {
        PersonalInfoManager pim = MoPub.getPersonalInformationManager();
        return pim != null ? pim.getConsentData() : NULL_CONSENT_DATA;
    }

    /**
     * @noinspection ALL
     */
    private static class NullConsentData implements ConsentData {

        @Override
        public String getCurrentVendorListVersion() {
            return null;
        }

        @Override
        public String getCurrentVendorListLink() {
            return null;
        }

        @Override
        public String getCurrentVendorListLink(String language) {
            return null;
        }

        @Override
        public String getCurrentPrivacyPolicyVersion() {
            return null;
        }

        @Override
        public String getCurrentPrivacyPolicyLink() {
            return null;
        }

        @Override
        public String getCurrentPrivacyPolicyLink(@Nullable String language) {
            return null;
        }

        @Override
        public String getCurrentVendorListIabFormat() {
            return null;
        }

        @Override
        public String getConsentedPrivacyPolicyVersion() {
            return null;
        }

        @Override
        public String getConsentedVendorListVersion() {
            return null;
        }

        @Override
        public String getConsentedVendorListIabFormat() {
            return null;
        }

        @Deprecated
        @Override
        public boolean isForceGdprApplies() {
            return false;
        }
    }
}
