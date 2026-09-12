using TMPro;
using UnityEngine;

namespace NextLevelGames
{
    public class Toast : MonoBehaviour
    {
        public static Toast Instance;
        public Animator toastAnim;
        public TMP_Text text;

        private void Awake()
        {
            if (Instance != null)
                Destroy(gameObject);
            else
                Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        public void ShowToast(string message)
        {
            AudioManager.Instance.Play("Toast");
            text.text = message;
            toastAnim.SetTrigger("Toast");
        }
    }
}
