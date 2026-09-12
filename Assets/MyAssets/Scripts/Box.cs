using DG.Tweening;
using UnityEngine;

namespace NextLevelGames
{
    public class Box : MonoBehaviour
    {
        public bool fallfromTable;
        public bool isJar;

        private void OnCollisionEnter(Collision collision)
        {
            if (!fallfromTable && collision.collider.CompareTag("Ground"))
            {
                if (isJar)
                {
                    AudioManager.Instance.Play("GlassJarHit");
                }
                else
                {
                    AudioManager.Instance.PlayBoxFallAudio();
                }

                transform.parent = null;

                fallfromTable = true;

                transform.DOScale(Vector3.zero, 1).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    Destroy(gameObject);
                });

                FindFirstObjectByType<LevelGenerator>().BoxDestroyed();
            }
        }
    }
}