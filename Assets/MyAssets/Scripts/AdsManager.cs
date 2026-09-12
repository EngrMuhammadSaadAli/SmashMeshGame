using UnityEngine;

namespace NextLevelGames
{
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance;

        public bool enableAds;

        public bool usingUnityAds;
        public bool usingAdmobAds;

        private UnityAdsManager unityAdsManager;
        private AdmobAdsManager admobAdsManager;

        private void Awake()
        {
            if (Instance != null)
                Destroy(gameObject);
            else
                Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            unityAdsManager = GetComponent<UnityAdsManager>();
            admobAdsManager = GetComponent<AdmobAdsManager>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                ShowBannerAd();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                ShowInterstitialAd();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                ShowRewardedAd();
            }
        }

        public void ShowBannerAd()
        {
            if (!enableAds)
                return;

            if (usingUnityAds)
                unityAdsManager.ShowBannerAd();
            else if (usingAdmobAds)
                admobAdsManager.ShowBannerAd();
        }

        public void ShowInterstitialAd()
        {
            if (!enableAds)
                return;

            if (usingUnityAds)
                unityAdsManager.ShowInterstitialAd();
            else if (usingAdmobAds)
                admobAdsManager.ShowInterstitialAd();
        }

        public void ShowRewardedAd()
        {
            if (!enableAds)
                return;

            if (usingUnityAds)
                unityAdsManager.ShowRewardedAd();
            else if (usingAdmobAds)
                admobAdsManager.ShowRewardedAd();
        }

        public void GiveReward()
        {
            CoinsManager.Instance.AddCoins(150);
        }
    }
}