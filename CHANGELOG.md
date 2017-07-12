## Version 4.15.0 (June 29, 2017)
- The MoPub Unity Plugin is now compatible with version 4.15.0 of the MoPub SDK.
- The MoPub Unity Plugin is now available on GitHub.

## Version 4.14.0
- The MoPub Unity Plugin is now compatible with version 4.14.0 of the MoPub SDK.

## Version 4.13.0
- The MoPub Unity Plugin is now compatible with version 4.13.0 of the MoPub SDK.

## Version 4.12.0
- The MoPub Unity Plugin is now compatible with version 4.12.0 of the MoPub SDK.

## Version 4.11.0
- Minor bug fixes

## Version 4.10.0
#### The Mopub Unity Plugin 4.10.0 introduces Modularized SDK for Android
Starting in the 4.10.0 release, we want to bring the modular SDK support to Android, so you choose to include specific ad formats and decrease overall SDK footprint in your app. For instance, if a publisher only wishes to integrate with MoPub’s interstitials and rewarded videos, you no longer need to include the banner SDK module to your project. For taking advantage of the new modular features, download the Android Unity package 4.10.0 from the "Getting Started" section and choose the modules of your choice from `Assets/Plugins/Android/mopub/libs/` directory:

- mopub-unity-plugins.jar: Unity plugins supporting banner, interstitial, and/or rewarded video (required)
- mopub-sdk-base.jar: base module for MoPub Android SDK (required)
- mopub-sdk-banner.jar: banner SDK module (optional)
- mopub-sdk-interstitial.jar: interstitial SDK module (optional, but required for rewarded video)
- mopub-sdk-rewardedvideo.jar: rewarded video SDK module (optional)

Note: interstitials is a prerequisite for rewarded videos, so one cannot integrate with rewarded videos without the interstitials SDK module.

## Version 4.9.0
- Support of multiple plugins for multiple banners, interstitials, and rewarded videos: one plugin per adunit
- Compatibility with MoPub's SDK 4.9.0 (modular SDK)
- Please note that there is no backward compatibility with SDK versions earlier than 4.9.0. For earlier versions, please check our reference documentation.
