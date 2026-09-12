using EasyUI.PickerWheelUI;
using NextLevelGames;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace NextLevelGames
{
    public class SpinWheelTimer : MonoBehaviour
    {
        private const string LAST_SPIN_TIME_KEY = "LastSpinTime"; // Save key
        private TimeSpan cooldownDuration = TimeSpan.FromHours(24); // 24-hour cooldown

        public Text timerText; // Reference to UI Text
        private bool isTimerRunning;
        public int spinCost;
        private PickerWheel pickerWheel;
        public GameObject winCoinsPanel;
        public Text winText;
        public Button spinCoinLessBtn, spinBtn, exitBtn;

        private void Start()
        {
            pickerWheel = FindFirstObjectByType<PickerWheel>();

            CheckSpinAvailability();
            InvokeRepeating(nameof(UpdateTimerDisplay), 0, 1f); // Update timer every second
        }

        public void SpinWheelOnCLick()
        {
            AudioManager.Instance.Play("Click");

            pickerWheel.OnSpinEnd(wheelPiece => {

                AudioManager.Instance.Play("Reward");

                CoinsManager.Instance.AddCoins(wheelPiece.Amount);
                winCoinsPanel.SetActive(true);
                winText.text = "+ " + wheelPiece.Amount;

                exitBtn.interactable = true;
                spinBtn.interactable = true;
                spinCoinLessBtn.interactable = true;
            });

            if (isTimerRunning)
            {
                if (CoinsManager.Instance.collectedCoins >= spinCost)
                {
                    CoinsManager.Instance.LessCoins(spinCost);
                    pickerWheel.Spin();
                    exitBtn.interactable = false;
                    spinBtn.interactable = false;
                    spinCoinLessBtn.interactable = false;
                }
                else
                    Toast.Instance.ShowToast("Not Enough Coins");
            }
            else
            {
                pickerWheel.Spin();

                exitBtn.interactable = false;
                spinBtn.interactable = false;
                spinCoinLessBtn.interactable = false;

                SecurePlayerPrefs.SetString(LAST_SPIN_TIME_KEY, DateTime.UtcNow.Ticks.ToString());
                SecurePlayerPrefs.SetInt(LAST_SPIN_TIME_KEY + "_ticks", (int)(DateTime.UtcNow.Ticks >> 32));
                CheckSpinAvailability();
            }
        }

        private void CheckSpinAvailability()
        {
            if (!SecurePlayerPrefs.HasKey(LAST_SPIN_TIME_KEY))
            {
                isTimerRunning = false;
                spinBtn.gameObject.SetActive(true);
                timerText.text = "Ready to Spin!";
                return;
            }

            spinCoinLessBtn.gameObject.SetActive(true);
            spinBtn.gameObject.SetActive(false);

            long lastSpinTicks = long.Parse(SecurePlayerPrefs.GetString(LAST_SPIN_TIME_KEY, "0"));
            DateTime lastSpinTime = new DateTime(lastSpinTicks);
            TimeSpan timePassed = DateTime.UtcNow - lastSpinTime;

            if (timePassed >= cooldownDuration)
            {
                isTimerRunning = false;
                timerText.text = "Ready to Spin!";
            }
            else
            {
                isTimerRunning = true;
                UpdateTimerDisplay();
            }
        }

        private void UpdateTimerDisplay()
        {
            if (!SecurePlayerPrefs.HasKey(LAST_SPIN_TIME_KEY)) return;

            long lastSpinTicks = long.Parse(SecurePlayerPrefs.GetString(LAST_SPIN_TIME_KEY, "0"));
            DateTime lastSpinTime = new DateTime(lastSpinTicks);
            TimeSpan timePassed = DateTime.UtcNow - lastSpinTime;

            if (timePassed >= cooldownDuration)
            {
                spinBtn.interactable = true;
                spinCoinLessBtn.interactable = true;

                timerText.text = "Ready to Spin!";

                spinBtn.gameObject.SetActive(true);
                spinCoinLessBtn.gameObject.SetActive(false);
            }
            else
            {
                TimeSpan remaining = cooldownDuration - timePassed;
                timerText.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        public void CloseWinPanel()
        {
            AudioManager.Instance.Play("Click");
            winCoinsPanel.SetActive(false);
        }
    }
}
