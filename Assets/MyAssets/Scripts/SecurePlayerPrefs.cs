using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NextLevelGames
{
    public static class SecurePlayerPrefs
    {
        private static readonly string SecretKey = "YourSecretKeyHere12345"; // Change this to your own secret key

        public static void SetInt(string key, int value)
        {
            string encryptedValue = EncryptInt(value);
            string checksum = GenerateChecksum(encryptedValue);
            PlayerPrefs.SetString(key + "_data", encryptedValue);
            PlayerPrefs.SetString(key + "_hash", checksum);
            PlayerPrefs.Save();
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            string encryptedValue = PlayerPrefs.GetString(key + "_data", null);
            string storedHash = PlayerPrefs.GetString(key + "_hash", null);

            if (string.IsNullOrEmpty(encryptedValue) || string.IsNullOrEmpty(storedHash))
            {
                return defaultValue;
            }

            string computedHash = GenerateChecksum(encryptedValue);
            if (computedHash != storedHash)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Data tampering detected for key: {key}, using default value");
#endif
                return defaultValue;
            }

            try
            {
                return DecryptInt(encryptedValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static void SetFloat(string key, float value)
        {
            string encryptedValue = EncryptFloat(value);
            string checksum = GenerateChecksum(encryptedValue);
            PlayerPrefs.SetString(key + "_data", encryptedValue);
            PlayerPrefs.SetString(key + "_hash", checksum);
            PlayerPrefs.Save();
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            string encryptedValue = PlayerPrefs.GetString(key + "_data", null);
            string storedHash = PlayerPrefs.GetString(key + "_hash", null);

            if (string.IsNullOrEmpty(encryptedValue) || string.IsNullOrEmpty(storedHash))
            {
                return defaultValue;
            }

            string computedHash = GenerateChecksum(encryptedValue);
            if (computedHash != storedHash)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Data tampering detected for key: {key}, using default value");
#endif
                return defaultValue;
            }

            try
            {
                return DecryptFloat(encryptedValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static void SetString(string key, string value)
        {
            string encryptedValue = EncryptString(value);
            string checksum = GenerateChecksum(encryptedValue);
            PlayerPrefs.SetString(key + "_data", encryptedValue);
            PlayerPrefs.SetString(key + "_hash", checksum);
            PlayerPrefs.Save();
        }

        public static string GetString(string key, string defaultValue = "")
        {
            string encryptedValue = PlayerPrefs.GetString(key + "_data", null);
            string storedHash = PlayerPrefs.GetString(key + "_hash", null);

            if (string.IsNullOrEmpty(encryptedValue) || string.IsNullOrEmpty(storedHash))
            {
                return defaultValue;
            }

            string computedHash = GenerateChecksum(encryptedValue);
            if (computedHash != storedHash)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Data tampering detected for key: {key}, using default value");
#endif
                return defaultValue;
            }

            try
            {
                return DecryptString(encryptedValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key + "_data") && PlayerPrefs.HasKey(key + "_hash");
        }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key + "_data");
            PlayerPrefs.DeleteKey(key + "_hash");
        }

        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        private static string EncryptInt(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        private static int DecryptInt(string encryptedValue)
        {
            byte[] bytes = Convert.FromBase64String(encryptedValue);
            return BitConverter.ToInt32(bytes, 0);
        }

        private static string EncryptFloat(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        private static float DecryptFloat(string encryptedValue)
        {
            byte[] bytes = Convert.FromBase64String(encryptedValue);
            return BitConverter.ToSingle(bytes, 0);
        }

        private static string EncryptString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        private static string DecryptString(string encryptedValue)
        {
            byte[] bytes = Convert.FromBase64String(encryptedValue);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string GenerateChecksum(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input + SecretKey));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
