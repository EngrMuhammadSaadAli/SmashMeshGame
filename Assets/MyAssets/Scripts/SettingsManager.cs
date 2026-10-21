using UnityEngine;
using UnityEngine.UI;
using RDG;

namespace NextLevelGames
{
    public class SettingsManager : MonoBehaviour
    {
        [Header("Settings Buttons")]
        public Button soundButton;
        public Button musicButton;
        public Button vibeButton;
        public GameObject soundOffLine;
        public GameObject musicOffLine;
        public GameObject vibeOffLine;

        private const string SoundKey = "SoundState";
        private const string MusicKey = "MusicState";
        private const string VibeKey = "VibrationState";

        private bool isSoundOn;
        private bool isMusicOn;
        public bool isVibeOn;

        void Start()
        {
            isSoundOn = SecurePlayerPrefs.GetInt(SoundKey, 1) == 1;
            isMusicOn = SecurePlayerPrefs.GetInt(MusicKey, 1) == 1;
            isVibeOn = SecurePlayerPrefs.GetInt(VibeKey, 1) == 1;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSoundEnabled(isSoundOn);
                AudioManager.Instance.SetMusicEnabled(isMusicOn);
            }

            UpdateToggleLines();
        }

        public void ToggleSound()
        {
            isSoundOn = !isSoundOn;
            SecurePlayerPrefs.SetInt(SoundKey, isSoundOn ? 1 : 0);

            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSoundEnabled(isSoundOn);

            UpdateToggleLines();
        }

        public void ToggleMusic()
        {
            isMusicOn = !isMusicOn;
            SecurePlayerPrefs.SetInt(MusicKey, isMusicOn ? 1 : 0);

            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicEnabled(isMusicOn);

            UpdateToggleLines();
        }

        public void ToggleVibration()
        {
            isVibeOn = !isVibeOn;
            SecurePlayerPrefs.SetInt(VibeKey, isVibeOn ? 1 : 0);

            UpdateToggleLines();

            if (isVibeOn)
                Vibration.Vibrate(30);
        }

        private void UpdateToggleLines()
        {
            if (soundOffLine != null)
                soundOffLine.SetActive(!isSoundOn);
            if (musicOffLine != null)
                musicOffLine.SetActive(!isMusicOn);
            if (vibeOffLine != null)
                vibeOffLine.SetActive(!isVibeOn);
        }
    }
}
