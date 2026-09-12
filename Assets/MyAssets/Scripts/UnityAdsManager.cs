using UnityEngine;
using UnityEngine.Advertisements;

namespace NextLevelGames
{
    public class UnityAdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsShowListener
    {
        string _androidGameId = "5929103";
        string _iOSGameId = "5929102";
        public bool _testMode = true;
        private string _gameId;

        string _androidBannerAdUnitId = "Banner_Android";
        string _iOSBannerAdUnitId = "Banner_iOS";

        string _androidInterAdUnitId = "Interstitial_Android";
        string _iOSInterAdUnitId = "Interstitial_iOS";

        string _androidRewardAdUnitId = "Rewarded_Android";
        string _iOSRewardAdUnitId = "Rewarded_iOS";

        string _adUnitId;

        void Start()
        {
            if (!AdsManager.Instance.usingUnityAds) return;

            _gameId = _androidGameId;

#if UNITY_IOS
                _gameId = _iOSGameId;
#endif

            if (!Advertisement.isInitialized && Advertisement.isSupported)
                Advertisement.Initialize(_gameId, _testMode, this);
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
#if UNITY_EDITOR
            Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
#endif

            if (!Advertisement.isInitialized && Advertisement.isSupported)
                Advertisement.Initialize(_gameId, _testMode);
        }

       // Load Banner
        private void LoadBannerAd()
        {
            // Set the banner position:
            Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);

            // Set up options to notify the SDK of load events:
            BannerLoadOptions options = new BannerLoadOptions
            {
                loadCallback = OnBannerLoaded,
                errorCallback = OnBannerError
            };

            // Get the Ad Unit ID for the current platform:
            _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _androidBannerAdUnitId
            : _iOSBannerAdUnitId;

#if UNITY_EDITOR
            Debug.Log("Loading Ad: " + _adUnitId);
#endif
            // Load the Ad Unit with banner content:
            Advertisement.Banner.Load(_adUnitId, options);
        }

        //Implement code to execute when the loadCallback event triggers:
        void OnBannerLoaded()
        {
#if UNITY_EDITOR
            Debug.Log("Banner loaded");
#endif
            ShowBannerAd();
        }

        // Implement code to execute when the load errorCallback event triggers:
        void OnBannerError(string message)
        {
#if UNITY_EDITOR
            Debug.Log($"Banner Error: {message}");
#endif
            // Optionally execute additional code, such as attempting to load another ad.
        }

        //Load Interstitial
        private void LoadInterAd()
        {
            // Get the Ad Unit ID for the current platform:
            _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSInterAdUnitId
            : _androidInterAdUnitId;

            // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
#if UNITY_EDITOR
            Debug.Log("Loading Ad: " + _adUnitId);
#endif
            Advertisement.Load(_adUnitId);
        }

        //Load Reward
        private void LoadRewardAd()
        {
            // Get the Ad Unit ID for the current platform:
            _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSRewardAdUnitId
            : _androidRewardAdUnitId;

            // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
#if UNITY_EDITOR
            Debug.Log("Loading Ad: " + _adUnitId);
#endif
            Advertisement.Load(_adUnitId);
        }

        //Show Banner
        public void ShowBannerAd()
        {
            // Get the Ad Unit ID for the current platform:
            _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _androidBannerAdUnitId
            : _iOSBannerAdUnitId;

            // Note that if the ad content wasn't previously loaded, this method will fail
#if UNITY_EDITOR
            Debug.Log("Showing Ad: " + _adUnitId);
#endif
            // Set up options to notify the SDK of show events:
            BannerOptions options = new BannerOptions
            {
                clickCallback = OnBannerClicked,
                hideCallback = OnBannerHidden,
                showCallback = OnBannerShown
            };

            // Show the loaded Banner Ad Unit:
            Advertisement.Banner.Show(_adUnitId, options);
        }

        void OnBannerClicked() { }
        void OnBannerShown() { }
        void OnBannerHidden() { }

        //Show Interstitial
        public void ShowInterstitialAd()
        {
            // Get the Ad Unit ID for the current platform:
            _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSInterAdUnitId
            : _androidInterAdUnitId;

            // Note that if the ad content wasn't previously loaded, this method will fail
#if UNITY_EDITOR
            Debug.Log("Showing Ad: " + _adUnitId);
#endif
            Advertisement.Show(_adUnitId, this);
        }

        //Show Reward
        public void ShowRewardedAd()
        {
            // Get the Ad Unit ID for the current platform:
            _adUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSRewardAdUnitId
            : _androidRewardAdUnitId;

            // Note that if the ad content wasn't previously loaded, this method will fail
#if UNITY_EDITOR
            Debug.Log("Showing Ad: " + _adUnitId);
#endif
            Advertisement.Show(_adUnitId, this);
        }

        //Implement the Show Listener's OnUnityAdsShowComplete callback method to determine if the user gets a reward:
        public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
        {
            if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
            {
#if UNITY_EDITOR
                Debug.Log("Unity Ads Rewarded Ad Completed");
#endif
                // Grant a reward.

                AdsManager.Instance.GiveReward();
            }
        }

        //Implement Load Listener and Show Listener interface methods :
        public void OnUnityAdsAdLoaded(string adUnitId)
        {
            // execute code if the Ad Unit successfully loads content.

            if (_adUnitId == _androidInterAdUnitId || _adUnitId == _iOSInterAdUnitId)
                ShowInterstitialAd();
            else if (_adUnitId == _androidRewardAdUnitId || _adUnitId == _iOSRewardAdUnitId)
                ShowRewardedAd();
        }

        public void OnUnityAdsFailedToLoad(string _adUnitId, UnityAdsLoadError error, string message)
        {
#if UNITY_EDITOR
            Debug.Log($"Error loading Ad Unit: {_adUnitId} - {error.ToString()} - {message}");
#endif
            // Optionally execute code if the Ad Unit fails to load, such as attempting to try again.

            if (_adUnitId == _androidBannerAdUnitId || _adUnitId == _iOSBannerAdUnitId)
                LoadBannerAd();
            else if (_adUnitId == _androidInterAdUnitId || _adUnitId == _iOSInterAdUnitId)
                LoadInterAd();
            else if (_adUnitId == _androidRewardAdUnitId || _adUnitId == _iOSRewardAdUnitId)
                LoadRewardAd();
        }

        public void OnUnityAdsShowFailure(string _adUnitId, UnityAdsShowError error, string message)
        {
#if UNITY_EDITOR
            Debug.Log($"Error showing Ad Unit {_adUnitId}: {error.ToString()} - {message}");
#endif
            // Optionally execute code if the Ad Unit fails to show, such as loading another ad.

            if (_adUnitId == _androidBannerAdUnitId || _adUnitId == _iOSBannerAdUnitId)
                LoadBannerAd();
            else if (_adUnitId == _androidInterAdUnitId || _adUnitId == _iOSInterAdUnitId)
                LoadInterAd();
            else if (_adUnitId == _androidRewardAdUnitId || _adUnitId == _iOSRewardAdUnitId)
                LoadRewardAd();
        }

        public void OnInitializationComplete()
        {
#if UNITY_EDITOR
            Debug.Log("Unity Ads initialization complete.");
#endif
        }

        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }
    }
}