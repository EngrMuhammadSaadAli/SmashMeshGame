using UnityEngine;
using UnityEngine.SceneManagement;

namespace NextLevelGames
{
    public class IAPShopManager : MonoBehaviour
    {
        private void Awake()
        {
            Time.timeScale = 1;
        }
        public void PurchaseComplete(int coins)
        {
            CoinsManager.Instance.AddCoins(coins);
            AudioManager.Instance.Play("Buy");
        }

        public void Home(int sceneIndex)
        {
            AudioManager.Instance.Play("Click");
            AdsManager.Instance.ShowInterstitialAd();
            SceneManager.LoadScene(sceneIndex);
        }
    }
}
