using NextLevelGames;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NextLevelGames
{
    public class CoinsManager : MonoBehaviour
    {
        public static CoinsManager Instance;

        [HideInInspector]
        public int collectedCoins;
        public Text collectedCoinsText;

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
            collectedCoins = SecurePlayerPrefs.GetInt("CoinsAmount", 100);
            ShowAndSave();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.M))
                AddCoins(50000);
#endif
        }

        public void AddCoins(int amount)
        {
            if (amount < 0)
                return;

            try
            {
                checked
                {
                    collectedCoins += amount;
                }
            }
            catch (System.OverflowException)
            {
                collectedCoins = int.MaxValue;
            }

            ShowAndSave();
        }

        public void LessCoins(int amount)
        {
            if (amount < 0)
                return;

            collectedCoins = System.Math.Max(0, collectedCoins - amount);
            ShowAndSave();
        }

        public void ShowAndSave()
        {
            SetCoinsText(collectedCoins);
            SecurePlayerPrefs.SetInt("CoinsAmount", collectedCoins);
        }

        public void SetCoinsText(int amount)
        {
            collectedCoinsText.text = amount.ToString();
        }
    }
}
