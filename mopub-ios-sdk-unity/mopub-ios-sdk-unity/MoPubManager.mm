//
//  MopubManager.m
//  MoPub
//
//  Copyright (c) 2017 __MyCompanyName__. All rights reserved.
//

#import "MoPubManager.h"
#import "MPAdConversionTracker.h"


#ifdef __cplusplus
extern "C" {
#endif
	// life cycle management
	void UnityPause( bool pause );
	void UnitySendMessage( const char* obj, const char* method, const char* msg );
#ifdef __cplusplus
}
#endif


@implementation MoPubManager

@synthesize adView = _adView, locationManager = _locationManager, lastKnownLocation = _lastKnownLocation, bannerPosition;

///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark NSObject

// Manager to be used for methods that do not require a specific adunit to operate on.
+ (MoPubManager*)sharedManager
{
    static MoPubManager *sharedManager = nil;

    if( !sharedManager )
        sharedManager = [[MoPubManager alloc] init];

    return sharedManager;
}

// Manager to be used for adunit specific methods
+ (MoPubManager*)managerForAdunit:(NSString *)adUnitId
{
	static NSMutableDictionary *managerDict = nil;
	
    if ( !managerDict ) {
		managerDict = [[NSMutableDictionary alloc] init];
    }

    MoPubManager *manager = [managerDict valueForKey:adUnitId];
    if ( !manager ) {
        manager = [[MoPubManager alloc] initWithAdUnit:adUnitId];
        managerDict[adUnitId] = manager;
    }

    return manager;
}


+ (UIViewController*)unityViewController
{
	return [[[UIApplication sharedApplication] keyWindow] rootViewController];
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark Private

- (void)adjustAdViewFrameToShowAdView
{
	// fetch screen dimensions and useful values
	CGRect origFrame = _adView.frame;

	CGFloat screenHeight = [UIScreen mainScreen].bounds.size.height;
	CGFloat screenWidth = [UIScreen mainScreen].bounds.size.width;

	// swap only on pre-iOS 8 when width is always less then height even in landscape
	if( UIInterfaceOrientationIsLandscape( [MoPubManager unityViewController].interfaceOrientation ) && screenWidth < screenHeight )
		screenHeight = [UIScreen mainScreen].bounds.size.width;
	
	
	switch( bannerPosition )
	{
		case MoPubAdPositionTopLeft:
			origFrame.origin.x = 0;
			origFrame.origin.y = 0;
			_adView.autoresizingMask = ( UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleBottomMargin );
			break;
		case MoPubAdPositionTopCenter:
			origFrame.origin.x = ( screenWidth / 2 ) - ( origFrame.size.width / 2 );
			origFrame.origin.y = 0;
			_adView.autoresizingMask = ( UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleBottomMargin );
			break;
		case MoPubAdPositionTopRight:
			origFrame.origin.x = screenWidth - origFrame.size.width;
			origFrame.origin.y = 0;
			_adView.autoresizingMask = ( UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleBottomMargin );
			break;
		case MoPubAdPositionCentered:
			origFrame.origin.x = ( screenWidth / 2 ) - ( origFrame.size.width / 2 );
			origFrame.origin.y = ( screenHeight / 2 ) - ( origFrame.size.height / 2 );
			_adView.autoresizingMask = ( UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleTopMargin | UIViewAutoresizingFlexibleBottomMargin );
			break;
		case MoPubAdPositionBottomLeft:
			origFrame.origin.x = 0;
			origFrame.origin.y = screenHeight - origFrame.size.height;
			_adView.autoresizingMask = ( UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin );
			break;
		case MoPubAdPositionBottomCenter:
			origFrame.origin.x = ( screenWidth / 2 ) - ( origFrame.size.width / 2 );
			origFrame.origin.y = screenHeight - origFrame.size.height;
			_adView.autoresizingMask = ( UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin );
			break;
		case MoPubAdPositionBottomRight:
			origFrame.origin.x = screenWidth - _adView.frame.size.width;
			origFrame.origin.y = screenHeight - origFrame.size.height;
			_adView.autoresizingMask = ( UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleTopMargin );
			break;
	}
	
	_adView.frame = origFrame;
    NSLog( @"setting adView frame: %@", NSStringFromCGRect( origFrame ) );
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark Public

- (id)initWithAdUnit:(NSString *)adUnitId
{
    self = [super init];
    if (self) {
        self->_adUnitId = adUnitId;
    }
    return self;
}

- (void)enableLocationSupport:(BOOL)shouldEnable
{
	if( _locationEnabled == shouldEnable )
		return;
	
	_locationEnabled = shouldEnable;
	
	// are we stopping or starting location use?
	if( _locationEnabled )
	{
		// autorelease and retain just in case we have an old one to avoid leaking
        self.locationManager = [[CLLocationManager alloc] init];
        self.locationManager.delegate = self;
        self.locationManager.distanceFilter = 100;
        self.locationManager.desiredAccuracy = kCLLocationAccuracyBest;
		
		// Make sure the user has location on in settings
		if( [CLLocationManager locationServicesEnabled] )
		{
			// Only start updating if we can get location information
			[self.locationManager startUpdatingLocation];
		}
		else
		{
			_locationEnabled = NO;
			self.locationManager = nil;
		}
	}
	else // turning off
	{
		[self.locationManager stopUpdatingLocation];
		self.locationManager.delegate = nil;
		self.locationManager = nil;
	}
}


- (void)reportApplicationOpen:(NSString*)iTunesId
{
	[[MPAdConversionTracker sharedConversionTracker] reportApplicationOpenForApplicationID:iTunesId];
}


- (void)createBanner:(MoPubBannerType)bannerType atPosition:(MoPubAdPosition)position
{
	// kill the current adView if we have one
	if( _adView )
		[self hideBanner:YES];
	
	bannerPosition = position;
	
	switch( bannerType )
	{
		case MoPubBannerType_320x50:
		{
			_adView = [[MPAdView alloc] initWithAdUnitId:_adUnitId size:MOPUB_BANNER_SIZE];
			[_adView lockNativeAdsToOrientation:MPNativeAdOrientationPortrait];
			break;
		}
		case MoPubBannerType_728x90:
		{
			_adView = [[MPAdView alloc] initWithAdUnitId:_adUnitId size:MOPUB_LEADERBOARD_SIZE];
			[_adView lockNativeAdsToOrientation:MPNativeAdOrientationPortrait];
			break;
		}
		case MoPubBannerType_160x600:
		{
			_adView = [[MPAdView alloc] initWithAdUnitId:_adUnitId size:MOPUB_WIDE_SKYSCRAPER_SIZE];
			break;
		}
		case MoPubBannerType_300x250:
		{
			_adView = [[MPAdView alloc] initWithAdUnitId:_adUnitId size:MOPUB_MEDIUM_RECT_SIZE];
			break;
		}
	}
	
	// do we have location enabled?
	if( _locationEnabled && _lastKnownLocation )
		_adView.location = _lastKnownLocation;
	
	_adView.delegate = self;
	[[MoPubManager unityViewController].view addSubview:_adView];
	[_adView loadAd];
}


- (void)destroyBanner
{
	[_adView removeFromSuperview];
	_adView.delegate = nil;
	self.adView = nil;
}


- (void)showBanner
{
	if( !_adView )
		return;
	
	_adView.hidden = NO;
    [_adView startAutomaticallyRefreshingContents];
}


- (void)hideBanner:(BOOL)shouldDestroy
{
	_adView.hidden = YES;
    [_adView stopAutomaticallyRefreshingContents];
	
	if( shouldDestroy )
		[self destroyBanner];
}


- (void)refreshAd:(NSString*)keywords
{
	if( !_adView )
		return;
	
	if( keywords )
		_adView.keywords = keywords;
    [_adView loadAd];
}


- (void)requestInterstitialAd:(NSString*)keywords
{
	// this will return nil if there is already a load in progress
	MPInterstitialAdController *interstitial = [MPInterstitialAdController interstitialAdControllerForAdUnitId:_adUnitId];
	
	if( _locationEnabled && _lastKnownLocation )
		interstitial.location = _lastKnownLocation;
	
	interstitial.keywords = keywords;
	interstitial.delegate = self;
	[interstitial loadAd];
}


- (void)showInterstitialAd
{
	MPInterstitialAdController *interstitial = [MPInterstitialAdController interstitialAdControllerForAdUnitId:_adUnitId];
	interstitial.delegate = self;
	if( !interstitial.ready )
	{
		NSLog( @"interstitial ad is not yet loaded" );
		return;
	}
	
	[interstitial showFromViewController:[MoPubManager unityViewController]];
}




///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - MPAdViewDelegate

- (UIViewController*)viewControllerForPresentingModalView 
{
    return [MoPubManager unityViewController];
}


/*
 * These callbacks notify you regarding whether the ad view (un)successfully
 * loaded an ad.
 */
- (void)adViewDidFailToLoadAd:(MPAdView*)view
{
	_adView.hidden = YES;
	UnitySendMessage( "MoPubManager", "onAdFailed", "" );
}


- (void)adViewDidLoadAd:(MPAdView*)view
{
	// resize the banner
	CGRect newFrame = _adView.frame;
	newFrame.size = _adView.adContentViewSize;
	_adView.frame = newFrame;

	[self adjustAdViewFrameToShowAdView];
	_adView.hidden = NO;
	
	UnitySendMessage( "MoPubManager", "onAdLoaded", [NSString stringWithFormat:@"%f", _adView.frame.size.height].UTF8String );
}


/*
 * These callbacks are triggered when the ad view is about to present/dismiss a
 * modal view. If your application may be disrupted by these actions, you can
 * use these notifications to handle them (for example, a game might need to
 * pause/unpause).
 */
- (void)willPresentModalViewForAd:(MPAdView*)view
{
	NSLog( @"willPresentModalViewForAd" );
	UnitySendMessage( "MoPubManager", "onAdExpanded", "" );
	UnityPause( true );
}


- (void)didDismissModalViewForAd:(MPAdView*)view
{
	NSLog( @"didDismissModalViewForAd" );
	UnitySendMessage( "MoPubManager", "onAdCollapsed", "" );
	UnityPause( false );
}


/*
 * This callback is triggered when the ad view has retrieved ad parameters
 * (headers) from the MoPub server. See MPInterstitialAdController for an
 * example of how this should be used.
- (void)adView:(MPAdView*)view didReceiveResponseParams:(NSDictionary*)params
{
	
}
*/


- (void)adViewShouldClose:(MPAdView*)view
{
	NSLog( @"adViewShouldClose" );
	UnityPause( false );
	[self hideBanner:YES];
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - MPInterstitialAdControllerDelegate

- (void)interstitialDidLoadAd:(MPInterstitialAdController*)interstitial
{
	UnitySendMessage( "MoPubManager", "onInterstitialLoaded", interstitial.adUnitId.UTF8String );
}


- (void)interstitialDidFailToLoadAd:(MPInterstitialAdController*)interstitial
{
	UnitySendMessage( "MoPubManager", "onInterstitialFailed", interstitial.adUnitId.UTF8String );
}


- (void)interstitialDidExpire:(MPInterstitialAdController*)interstitial
{
	UnitySendMessage( "MoPubManager", "interstitialDidExpire", interstitial.adUnitId.UTF8String );
}


- (void)interstitialWillAppear:(MPInterstitialAdController*)interstitial
{
	UnityPause( true );
	UnitySendMessage( "MoPubManager", "onInterstitialShown", interstitial.adUnitId.UTF8String );
}


- (void)interstitialWillDisappear:(MPInterstitialAdController*)interstitial
{
	NSLog( @"interstitialWillDisappear" );
	UnityPause( false );
}


- (void)interstitialDidDisappear:(MPInterstitialAdController*)interstitial
{
	NSLog( @"interstitialDidDisappear" );
	UnityPause( false );
	UnitySendMessage( "MoPubManager", "onInterstitialDismissed", interstitial.adUnitId.UTF8String );
}


- (void)interstitialDidReceiveTapEvent:(MPInterstitialAdController *)interstitial
{
	NSLog( @"interstitialDidReceiveTapEvent" );
	UnitySendMessage( "MoPubManager", "onInterstitialClicked", interstitial.adUnitId.UTF8String );
}



///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - CLLocationManagerDelegate

- (void)locationManager:(CLLocationManager*)manager didUpdateToLocation:(CLLocation*)newLocation fromLocation:(CLLocation*)oldLocation
{
	// update our locations
	if( _adView )
		_adView.location = newLocation;
	
	self.lastKnownLocation = newLocation;
}



///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - MPRewardedVideoDelegate

- (void)rewardedVideoAdDidLoadForAdUnitID:(NSString *)adUnitID
{
	NSLog( @"rewardedVideoAdDidLoadForAdUnitID" );
	UnitySendMessage( "MoPubManager", "onRewardedVideoLoaded", adUnitID.UTF8String );
}


- (void)rewardedVideoAdDidFailToLoadForAdUnitID:(NSString *)adUnitID error:(NSError *)error
{
	NSLog( @"rewardedVideoAdDidFailToLoadForAdUnitID error: %@", error );
	UnitySendMessage( "MoPubManager", "onRewardedVideoFailed", adUnitID.UTF8String );
}


- (void)rewardedVideoAdDidExpireForAdUnitID:(NSString *)adUnitID
{
	NSLog( @"rewardedVideoAdDidExpireForAdUnitID" );
	UnitySendMessage( "MoPubManager", "onRewardedVideoExpired", adUnitID.UTF8String );
}


- (void)rewardedVideoAdDidFailToPlayForAdUnitID:(NSString *)adUnitID error:(NSError *)error
{
	NSLog( @"rewardedVideoAdDidFailToPlayForAdUnitID" );
	UnitySendMessage( "MoPubManager", "onRewardedVideoFailedToPlay", adUnitID.UTF8String );
}


- (void)rewardedVideoAdWillAppearForAdUnitID:(NSString *)adUnitID
{
	NSLog( @"rewardedVideoAdWillAppearForAdUnitID" );
	UnityPause( true );
	UnitySendMessage( "MoPubManager", "onRewardedVideoShown", adUnitID.UTF8String );
}


//- (void)rewardedVideoAdDidAppearForAdUnitID:(NSString *)adUnitID;

- (void)rewardedVideoAdDidDisappearForAdUnitID:(NSString *)adUnitID
{
    NSLog( @"rewardedVideoAdDidDisappearForAdUnitID" );
	UnityPause( false );
	UnitySendMessage( "MoPubManager", "onRewardedVideoClosed", adUnitID.UTF8String );
}


//- (void)rewardedVideoAdDidDisappearForAdUnitID:(NSString *)adUnitID;

- (void)rewardedVideoAdDidReceiveTapEventForAdUnitID:(NSString *)adUnitID
{
    NSLog( @"rewardedVideoAdDidReceiveTapEventForAdUnitID" );
    UnitySendMessage( "MoPubManager", "onRewardedVideoClicked", adUnitID.UTF8String );
}

- (void)rewardedVideoAdWillLeaveApplicationForAdUnitID:(NSString *)adUnitID
{
	NSLog( @"rewardedVideoAdWillLeaveApplicationForAdUnitID" );
	UnitySendMessage( "MoPubManager", "onRewardedVideoLeavingApplication", adUnitID.UTF8String );
}


- (void)rewardedVideoAdShouldRewardForAdUnitID:(NSString *)adUnitID reward:(MPRewardedVideoReward *)reward
{
	NSLog( @"rewardedVideoAdShouldRewardForAdUnitID" );
	NSDictionary *dict = @{
		@"adUnitId": adUnitID,
		@"currencyType": reward.currencyType,
		@"amount": reward.amount
	};
	NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dict options:0 error:nil];
	NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
	UnitySendMessage( "MoPubManager", "onRewardedVideoReceivedReward", jsonString.UTF8String );
}

@end
