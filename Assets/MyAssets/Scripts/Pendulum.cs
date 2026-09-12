using UnityEngine;

namespace NextLevelGames
{
    public class Pendulum : MonoBehaviour
    {
        [Header("Pendulum Settings")]
        [Tooltip("The maximum angle of the swing in degrees.")]
        public float maxAngle = 45f;

        [Tooltip("How fast the pendulum swings.")]
        public float speed = 2f;

        [Tooltip("Offsets the starting point of the swing (0 to 2*PI).")]
        public float phaseOffset = 0f;

        private Quaternion startRotation;

        void Start()
        {
            // Store the initial rotation so we swing relative to how it's placed in the scene
            startRotation = transform.localRotation;
        }

        void Update()
        {
            // Calculate the angle using a sine wave
            // Time.time * speed advances the wave, phaseOffset lets you desynchronize multiple pendulums
            float angle = maxAngle * Mathf.Sin(Time.time * speed + phaseOffset);

            // Apply the rotation strictly on the Z axis relative to the starting rotation
            transform.localRotation = startRotation * Quaternion.Euler(0, 0, angle);
        }
    }
}