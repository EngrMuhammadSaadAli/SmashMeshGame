using UnityEngine;
using UnityEngine.UI;
using RDG;

namespace NextLevelGames
{
    public class SettingsManager : MonoBehaviour
    {
        [Header("Sound Settings")]
        public Button soundButton;
        public Image soundButtonImage;
        public Sprite soundOnSprite;
        public Sprite soundOffSprite;

        [Header("Vibration Settings")]
        public Button vibeButton;
        public Image vibeButtonImage;
        public Sprite vibeOnSprite;
        public Sprite vibeOffSprite;

        // Keys for saving data
        private const string SoundKey = "SoundState";
        private const string VibeKey = "VibrationState";

        // State variables
        private bool isSoundOn;
        public bool isVibeOn;

        void Start()
        {
            // Load saved settings (Default is 1, meaning ON)
            isSoundOn = SecurePlayerPrefs.GetInt(SoundKey, 1) == 1;
            isVibeOn = SecurePlayerPrefs.GetInt(VibeKey, 1) == 1;
            // Apply states and update UI on startup
            ApplySoundState();
            UpdateSoundUI();

            UpdateVibeUI(); // Vibration state doesn't have a global Unity toggle, just UI update
        }

        // --- SOUND LOGIC ---
        public void ToggleSound()
        {
            Debug.Log("ToggleSound");
            isSoundOn = !isSoundOn; // Flip the state
            SecurePlayerPrefs.SetInt(SoundKey, isSoundOn ? 1 : 0);

            ApplySoundState();
            UpdateSoundUI();
        }

        private void ApplySoundState()
        {
            // Mutes or unmutes all audio in the game globally
            AudioListener.volume = isSoundOn ? 1f : 0f;
        }

        private void UpdateSoundUI()
        {
            if (soundButtonImage != null)
            {
                soundButtonImage.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
            }
        }

        // --- VIBRATION LOGIC ---
        public void ToggleVibration()
        {
            isVibeOn = !isVibeOn; // Flip the state
            SecurePlayerPrefs.SetInt(VibeKey, isVibeOn ? 1 : 0);

            UpdateVibeUI();

            // Play a tiny vibration when turned ON to give feedback to the user
            if (isVibeOn)
            {
                Vibration.Vibrate(30);
            }
        }

        private void UpdateVibeUI()
        {
            if (vibeButtonImage != null)
            {
                vibeButtonImage.sprite = isVibeOn ? vibeOnSprite : vibeOffSprite;
            }
        }
    }
}