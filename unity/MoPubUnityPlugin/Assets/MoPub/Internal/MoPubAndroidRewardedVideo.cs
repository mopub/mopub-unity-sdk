using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_ANDROID

using MoPubReward = MoPubManager.MoPubReward;

public class MoPubAndroidRewardedVideo
{
	private static readonly AndroidJavaClass _pluginClass =
		new AndroidJavaClass ("com.mopub.unity.MoPubRewardedVideoUnityPlugin");
	private readonly AndroidJavaObject _plugin;
	private Dictionary<MoPubReward, AndroidJavaObject> _rewardsDict = new Dictionary<MoPubReward, AndroidJavaObject>();
	private MoPubReward _selectedReward;


	public MoPubAndroidRewardedVideo (string adUnitId)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		_plugin = new AndroidJavaObject ("com.mopub.unity.MoPubRewardedVideoUnityPlugin", adUnitId);
	}


	// Initializes the rewarded video system
	public static void initializeRewardedVideo ()
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		_pluginClass.CallStatic ("initializeRewardedVideo");
	}


	// Initializes the rewarded video system with desired networks
	public static void initializeRewardedVideoWithNetworks (MoPubRewardedNetwork[] networks)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		// Translate the array of networks into a comma-delimited string.
		string networksString = null;
		if (networks != null && networks.Length > 0) {
			networksString = string.Join(",", Array.ConvertAll(networks, x => x.ToString()));
		}

		_pluginClass.CallStatic ("initializeRewardedVideoWithNetworks", networksString);
	}


	// Starts loading a rewarded video ad
	public void requestRewardedVideo (List<MoPubMediationSetting> mediationSettings = null,
		string keywords = null,
		double latitude = MoPub.LAT_LONG_SENTINEL,
		double longitude = MoPub.LAT_LONG_SENTINEL,
		string customerId = null)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		var json = (mediationSettings == null) ?
			null :
			MoPubInternal.ThirdParty.MiniJSON.Json.Serialize (mediationSettings);
		_plugin.Call ("requestRewardedVideo", json, keywords, latitude, longitude, customerId);
	}


	// If a rewarded video ad is loaded this will take over the screen and show the ad
	public void showRewardedVideo (string customData)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		_plugin.Call ("showRewardedVideo", customData);
	}


	// Whether a rewarded video is ready to play
	public bool hasRewardedVideo()
	{
		if (Application.platform != RuntimePlatform.Android)
			return false;

		return _plugin.Call<bool> ("hasRewardedVideo");
	}


	// Retrieves a list of available rewards for this AdUnit
	public List<MoPubReward> getAVailableRewards()
	{
		if (Application.platform != RuntimePlatform.Android)
			return null;

		// Clear any existing reward object mappings between Unity and Android Java
		_rewardsDict.Clear();

		using (AndroidJavaObject obj = _plugin.Call<AndroidJavaObject> ("getAvailableRewards")) {
			AndroidJavaObject[] rewardsJavaObjArray =
					AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]> (obj.GetRawObject ());

			if (rewardsJavaObjArray.Length > 1) {
				foreach (AndroidJavaObject r in rewardsJavaObjArray) {
					string label = r.Call<string> ("getLabel");
					int amount = r.Call<int> ("getAmount");
					_rewardsDict.Add (new MoPubReward (label, amount), r);
				}
			}
		}

		return new List<MoPubReward> (_rewardsDict.Keys);
	}


	// Selects the reward for this AdUnit
	public void selectReward(MoPubReward selectedReward)
	{
		if (Application.platform != RuntimePlatform.Android)
			return;

		AndroidJavaObject rewardJavaObj;
		if (_rewardsDict.TryGetValue(selectedReward, out rewardJavaObj)) {
			_plugin.Call ("selectReward", rewardJavaObj);
		} else {
			Debug.LogWarning (String.Format ("Selected reward {0} is not available.", selectedReward));
		}
	}
}

#endif
