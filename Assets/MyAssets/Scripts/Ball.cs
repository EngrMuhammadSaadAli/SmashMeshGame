using UnityEngine;
using RDG;

namespace NextLevelGames
{
    public class Ball : MonoBehaviour
    {
        private SettingsManager settingsManager;
        bool isObjectCollided = true;

        private void Start()
        {
            settingsManager = FindFirstObjectByType<SettingsManager>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (isObjectCollided && other.collider.CompareTag("Object"))
            {
                if(other.collider.gameObject.GetComponent<Box>().isJar)
                {
                    AudioManager.Instance.Play("GlassJarHit");
                }

                if (settingsManager.isVibeOn)
                    Vibration.Vibrate(20);
                

                isObjectCollided = false;
                GameObject ballHitParticlePrefab = Resources.Load<GameObject>("BallHitParticle");
                Instantiate(ballHitParticlePrefab, other.contacts[0].point, Quaternion.identity);
                FindFirstObjectByType<LevelGenerator>().UnfreezeAllBoxes(true);
            }
            else if(other.collider.CompareTag("Obstacle"))
            {
                if (settingsManager.isVibeOn)                
                    Vibration.Vibrate(70);

                AudioManager.Instance.Play("ObstacleHit");
            }
        }
    }
}