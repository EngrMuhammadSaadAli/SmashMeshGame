using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NextLevelGames
{
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private float loadingDuration = 5f;
        [SerializeField] private float logoPulseAmount = 0.045f;
        [SerializeField] private float logoPulseSpeed = 2.2f;

        private Slider loadingSlider;
        private Text legacyLoadingText;
        private TMP_Text loadingText;
        private Transform logo;
        private Vector3 logoBaseScale;

        private void Awake()
        {
            loadingSlider = GetComponentInChildren<Slider>(true);
            legacyLoadingText = GetComponentInChildren<Text>(true);
            loadingText = GetComponentInChildren<TMP_Text>(true);

            var logoObject = transform.Find("Image");
            if (logoObject != null)
            {
                logo = logoObject;
                logoBaseScale = logo.localScale;
            }

            if (loadingSlider != null)
            {
                loadingSlider.minValue = 0f;
                loadingSlider.maxValue = 1f;
                loadingSlider.value = 0f;
                loadingSlider.interactable = false;
            }

        }

        private void Start()
        {
            StartCoroutine(LoadGame());
        }

        private void Update()
        {
            if (logo == null)
                return;

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * logoPulseSpeed) * logoPulseAmount;
            logo.localScale = logoBaseScale * pulse;
            logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * logoPulseSpeed * 0.65f) * 3f);
        }

        private IEnumerator LoadGame()
        {
            SetProgress(0f);
            AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
            if (operation == null)
            {
                Debug.LogError($"Could not load scene '{gameSceneName}'. Check that it is included in Build Settings.");
                yield break;
            }

            operation.allowSceneActivation = false;
            float loadingStartTime = Time.unscaledTime;
            while (operation.progress < 0.9f || Time.unscaledTime - loadingStartTime < loadingDuration)
            {
                float timedProgress = loadingDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01((Time.unscaledTime - loadingStartTime) / loadingDuration);
                // The scene may load much faster than the display duration. Keep the
                // bar moving from zero at a steady rate, then wait at 99% if needed.
                SetProgress(Mathf.Min(timedProgress, 0.99f));
                yield return null;
            }

            SetProgress(1f);
            operation.allowSceneActivation = true;
        }

        private void SetProgress(float progress)
        {
            if (loadingSlider != null)
                loadingSlider.SetValueWithoutNotify(progress);

            if (legacyLoadingText != null)
                legacyLoadingText.text = $"{Mathf.RoundToInt(progress * 100f)}%";

            if (loadingText != null)
                loadingText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }
    }
}
