using UnityEngine;
using GoogleMobileAds.Api;

namespace NextLevelGames
{
    public class AdmobAdsManager : MonoBehaviour
    {
        private string intertestialId = "ca-app-pub-3940256099942544/1033173712";
        private string bannerId = "ca-app-pub-3940256099942544/6300978111";
        private string rewardId = "ca-app-pub-3940256099942544/5224354917";

        private InterstitialAd interstitialAd;
        private BannerView bannerView;
        private RewardedAd rewardedAd;

        void Start()
        {
            if (!AdsManager.Instance.usingAdmobAds) return;

            MobileAds.Initialize(initStatus => { });
            LoadInterstitialAd();
            LoadRewardedAd();
        }

        public void ShowBannerAd()
        {
            if (bannerView == null)
                CreateBannerView();

            var adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            bannerView.LoadAd(adRequest);
        }

        private void CreateBannerView()
        {

            if (bannerView != null)
                DestroyAd();

            bannerView = new BannerView(bannerId, AdSize.Banner, AdPosition.Bottom);
        }

        private void DestroyAd()
        {
            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
            }
        }

        private void LoadInterstitialAd()
        {
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }


            var adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            InterstitialAd.Load(intertestialId, adRequest,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {

                        return;
                    }


                    interstitialAd = ad;
                });
        }

        public void ShowInterstitialAd()
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
#if UNITY_EDITOR
                Debug.Log("Showing interstitial ad.");
#endif
                interstitialAd.Show();
            }

            LoadInterstitialAd();
        }

        private void LoadRewardedAd()
        {
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

#if UNITY_EDITOR
            Debug.Log("Loading the rewarded ad.");
#endif

            var adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            RewardedAd.Load(rewardId, adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
#if UNITY_EDITOR
                        Debug.LogError("Rewarded ad failed to load an ad " +
                                       "with error : " + error);
#endif
                        return;
                    }

#if UNITY_EDITOR
                    Debug.Log("Rewarded ad loaded with response : "
                              + ad.GetResponseInfo());
#endif

                    rewardedAd = ad;
                });
        }

        public void ShowRewardedAd()
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show((Reward reward) =>
                {
                    AdsManager.Instance.GiveReward();
                });
            }

            LoadRewardedAd();
        }
    }
}