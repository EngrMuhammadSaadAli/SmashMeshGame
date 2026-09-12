using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NextLevelGames
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject retryPanel, winPanel, startPanel, commingSoonPanel, spinWheelPanel, settingPanel;
        private bool isWin, isRetry;
        private int levelNo;
        public int skipLevelCoinAmount;
        public bool isGamePlaying;
        public TMP_Text[] levelTexts;

#if UNITY_EDITOR
        public int testLevelNo;
#endif
        void Awake()
        {
            Time.timeScale = 1;
            Application.targetFrameRate = 60;
            levelNo = SecurePlayerPrefs.GetInt("Level", 1);
            UpdateLevelUI();

            FindFirstObjectByType<LevelGenerator>().GenerateLevel(levelNo);
        }

        private void UpdateLevelUI()
        {
            for (int i = 0; i < levelTexts.Length; i++)
            {
                int displayLevel = levelNo + i;

                if (displayLevel <= 1000)
                {
                    levelTexts[i].text = displayLevel.ToString();
                }
                else
                {
                    levelTexts[i].text = "COMING\nSOON";
                    levelTexts[i].fontSize = 23;
                }
            }
        }

        private void Update()
        {

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.P))
            {
                SecurePlayerPrefs.DeleteAll();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(0);
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                Next();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                SecurePlayerPrefs.SetInt("Level", levelNo - 1);
                SceneManager.LoadScene(0);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                SecurePlayerPrefs.SetInt("Level", testLevelNo);
                SceneManager.LoadScene(0);
            }    
#endif
        }

        public void Retry()
        {
            if (isWin || isRetry)
                return;

            isRetry = true;
            Invoke(nameof(InvokeRetryGame), .5f);
        }

        private void InvokeRetryGame()
        {
            isGamePlaying = false;

            AudioManager.Instance.Play("Retry");
            retryPanel.SetActive(true);
            retryPanel.transform.DOScale(new Vector3(1, 1, 1), .5f);
        }

        public void Reload()
        {
            PlayClickSound();
            AdsManager.Instance.ShowInterstitialAd();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void StartGame()
        {
            PlayClickSound();
            isGamePlaying = true;
            FindFirstObjectByType<CannonShooter>().enabled = true;
            startPanel.SetActive(false);
        }

        public void Win()
        {
            if (isRetry || isWin)
                return;

            isWin = true;
            Invoke(nameof(InvokeWinGame), 2);
        }
        private void InvokeWinGame()
        {
            isGamePlaying = false;

            AudioManager.Instance.Play("Win");

            if (levelNo < 1000)
            {
                winPanel.SetActive(true);
                winPanel.transform.DOScale(new Vector3(1, 1, 1), .5f);
            }
            else
            {
                commingSoonPanel.SetActive(true);
                commingSoonPanel.transform.DOScale(new Vector3(1, 1, 1), .5f);
            }
        }

        public void Next()
        {
            PlayClickSound();
            AdsManager.Instance.ShowInterstitialAd();

            SecurePlayerPrefs.SetInt("Level", levelNo + 1);
            SceneManager.LoadScene(0);
        }

        public void ResetGame()
        {
            SecurePlayerPrefs.DeleteAll();
            SceneManager.LoadScene(0);
        }

        public void ShowRewardVideo()
        {
            AdsManager.Instance.ShowRewardedAd();
        }

        public void OpenScene(int index)
        {
            PlayClickSound();
            AdsManager.Instance.ShowInterstitialAd();
            SceneManager.LoadScene(index);
        }

        public void SkipLevel()
        {
            PlayClickSound();
            AdsManager.Instance.ShowInterstitialAd();

            if (CoinsManager.Instance.collectedCoins >= skipLevelCoinAmount)
            {
                CoinsManager.Instance.LessCoins(skipLevelCoinAmount);
                Next();
                AudioManager.Instance.Play("Buy");
            }
            else
            {
                Toast.Instance.ShowToast("Not Enough Coins");
            }
        }

        public void SetActiveSpinWheelPanel(bool val)
        {
            PlayClickSound();
            spinWheelPanel.SetActive(val);
        }

        public void SetActiveSettingPanel(bool val)
        {
            PlayClickSound();
            settingPanel.SetActive(val);
        }
       
        public void PlayClickSound()
        {
            AudioManager.Instance.Play("Click");
        }
    }
}