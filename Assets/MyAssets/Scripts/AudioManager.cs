using System;
using UnityEngine;

namespace NextLevelGames
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        public AudioClip[] audioClips;
        public AudioClip[] boxFallAudioClips;

        private void Awake()
        {
            if (Instance != null)
                Destroy(gameObject);
            else
                Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        public void Play(string name)
        {
            AudioClip clip = Array.Find(audioClips, sound => sound.name == name);

            if (clip == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Clip not found: " + name);
#endif
                return;
            }

            AudioSource tempAudioSource = gameObject.AddComponent<AudioSource>();

            if (name == "Coin")
                tempAudioSource.volume = .2f;

            tempAudioSource.clip = clip;
            tempAudioSource.Play();
            Destroy(tempAudioSource, clip.length);
        }

        public void PlayBoxFallAudio()
        {
            AudioClip clip = boxFallAudioClips[UnityEngine.Random.Range(0, boxFallAudioClips.Length)];

            AudioSource tempAudioSource = gameObject.AddComponent<AudioSource>();
            tempAudioSource.clip = clip;
            tempAudioSource.Play();
            Destroy(tempAudioSource, clip.length);
        }
    }
}