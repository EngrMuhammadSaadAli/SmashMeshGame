using RDG;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace NextLevelGames
{
    /// <summary>
    /// Aims the cannon at the mouse cursor, fires balls at it on mouse-up, and
    /// tracks the remaining ball count / when to trigger a retry check once the
    /// player is out of balls and the boxes have settled.
    /// </summary>
    public class CannonShooter : MonoBehaviour
    {
        // cannonPivot: the part that rotates to aim (should NOT be the whole rig
        // if other parts of the cannon need to stay fixed).
        // shootPointTra: empty transform at the barrel tip balls are spawned from.
        public Transform cannonPivot, shootPointTra;
        public GameObject ballPrefab;

        // How many shots the player has left this level. Set externally by
        // LevelGenerator from the level's moveCount when a level is generated.
        public int maxBalls;
        public TMP_Text ballsRemainingText;

        // Launch speed applied to each ball (see TryShoot).
        public float force;

        // How far along the camera's forward axis (in ScreenToWorldPoint terms)
        // the aim point is projected when computing the shot's target direction.
        public float distance;

        public Animator cannonAnim;
        public ParticleSystem shootParticle;

        // Cached reference so isGamePlaying/Retry() don't need FindFirstObjectByType
        // every frame � only looked up once in Start.
        private GameManager gameManager;
        private SettingsManager settingsManager;
        private LevelGenerator levelGenerator;

        // --- Box motion tracking (used by CheckRetryInvoke / IsBoxMoving) ---
        // Stores each box's last-known local position/rotation relative to its
        // parent, so we can detect motion relative to a parent (e.g. a tray or
        // table that itself moves/rotates) instead of just raw world velocity.
        private readonly Dictionary<Box, Vector3> lastLocalPositions = new Dictionary<Box, Vector3>();
        private readonly Dictionary<Box, Quaternion> lastLocalRotations = new Dictionary<Box, Quaternion>();

        private const float positionDeltaThreshold = 0.001f;
        private const float rotationDeltaThreshold = 0.1f; // degrees

        private bool forceIncBtnClicked;

        private void Start()
        {
            force = SecurePlayerPrefs.GetFloat("Force", force);

            levelGenerator = FindFirstObjectByType<LevelGenerator>();
            settingsManager = FindFirstObjectByType<SettingsManager>();
            gameManager = FindFirstObjectByType<GameManager>();
            UpdateBallsUI();
        }

        private void Update()
        {
            // Aiming happens continuously so the cannon tracks the mouse even
            // while the player is deciding whether to shoot.
            AimAtMouse();

            // The shot itself only fires on mouse-up (not mouse-down), so the
            // player can drag around to aim before committing to the shot.
            if (Input.GetMouseButtonUp(0))
            {
                if (forceIncBtnClicked)
                {
                    forceIncBtnClicked = false;
                    return;
                }

                if (!levelGenerator.isBoxesUsingGravity)
                    DetectObjectAtMousePos();

                TryShoot();
            }
        }

        private void DetectObjectAtMousePos()
        {
            GameObject hitObject = GetHitObject();

            if (hitObject != null)
            {
                if (hitObject.CompareTag("Object"))
                    FindFirstObjectByType<LevelGenerator>().UnfreezeAllBoxes(false);
            }
        }

        private GameObject GetHitObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.SphereCast(ray, 0.01f, out RaycastHit hit, 100))
                return hit.collider.gameObject;

            return null;
        }

        private void AimAtMouse() // Aiming
        {
            Vector3 mousePos = Input.mousePosition;

            // 1. Calculate the target world position of the mouse.
            // The "10.0f" here is the distance from the camera along its forward
            // axis used to unproject the 2D mouse position into 3D world space �
            // it should roughly match how far the aiming plane/table sits from
            // the camera for the aim point to line up with what's visually under
            // the cursor.
            Vector3 targetWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10.0f));

            // 2. Cache the original Z rotation set from the Inspector (Euler angles).
            // This is the cannon's fixed "roll" � LookAt below will clobber it,
            // so we save it here to restore afterward.
            float originalZ = cannonPivot.localEulerAngles.z;
            // Note: Use cannonPivot.eulerAngles.z instead if your pivot is not a child of another GameObject

            // 3. Make the cannon look at the target momentarily to capture the new rotation.
            // LookAt sets X/Y/Z all at once, which is why step 2 saved Z beforehand.
            cannonPivot.LookAt(targetWorldPos);

            // 4. Blend the new look-at rotation (pitch/yaw from aiming) with the
            // fixed Inspector Z rotation (roll), so aiming never twists the
            // cannon's roll away from its intended fixed orientation.
            Vector3 currentEuler = cannonPivot.localEulerAngles;
            cannonPivot.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, originalZ);
        }

        private void TryShoot() // Shooting
        {
            // Block shots if the game isn't currently in the "playing" state
            // (e.g. paused, won, lost) or if the player has no balls left.
            if (!gameManager.isGamePlaying || maxBalls <= 0) return;

            // Feedback: haptics, sound, muzzle particle, and the fire animation.
            if (settingsManager.isVibeOn)
                Vibration.Vibrate(30);

            AudioManager.Instance.Play("Shoot");
            shootParticle.Play();

            maxBalls--;

            cannonAnim.Play("CannonRigShoot");

            // Unproject the mouse position into world space at "distance" units
            // from the camera � this becomes the point the ball is aimed at.
            var position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance);
            position = Camera.main.ScreenToWorldPoint(position);

            // Spawn the ball at the barrel tip (not at the aim point) � it
            // travels FROM shootPointTra TOWARD the aim point below.
            var go = Instantiate(ballPrefab, shootPointTra.position, Quaternion.identity) as GameObject;

            // Safety cleanup: if the ball never gets destroyed by gameplay
            // (missed everything, flew off-scene), it's removed after 5s so
            // balls don't pile up indefinitely.
            Destroy(go, 5f);

            // Orient the ball to face the aim point, then launch it straight
            // along its own forward direction at a fixed speed (force). This is
            // a simple direction-based shot (not a solved ballistic arc), so
            // gravity will still curve its actual flight path after launch.
            go.transform.LookAt(position);
            go.GetComponent<Rigidbody>().linearVelocity = (go.transform.forward * force);

            UpdateBallsUI();

            // Once the player is out of balls, wait a moment then start
            // polling whether the boxes have stopped moving, to decide if a
            // retry is needed (see CheckRetryInvoke). CancelInvoke first in
            // case this was already scheduled, so we don't double-schedule it.
            if (maxBalls == 0)
            {
                CancelInvoke(nameof(CheckRetryInvoke));
                Invoke(nameof(CheckRetryInvoke), 1);
            }
        }

        /// <summary>
        /// Polls all Box objects in the scene to see if any are still settling
        /// (moving, and not already fallen off the table). If everything has
        /// come to rest, triggers a retry via GameManager; otherwise checks
        /// again shortly after. This lets the player see the final result of
        /// their last shot (boxes still tumbling) before the retry kicks in.
        /// </summary>
        private void CheckRetryInvoke()
        {
            bool anyboxMoving = false;

            foreach (Box box in FindObjectsByType<Box>(FindObjectsSortMode.None))
            {
                // Boxes that already fell off the table are no longer relevant
                // to whether the remaining stack has settled.
                if (box.fallfromTable)
                    continue;

                if (IsBoxMoving(box))
                {
                    anyboxMoving = true;
                    break;
                }
            }

            if (anyboxMoving)
            {
                // Still settling � check again shortly rather than retrying immediately.
                Invoke(nameof(CheckRetryInvoke), .5f);
            }
            else
            {
                // Clear tracked state so stale entries don't leak into the next round.
                lastLocalPositions.Clear();
                lastLocalRotations.Clear();
                FindFirstObjectByType<GameManager>().Retry();
            }
        }

        /// <summary>
        /// True if the box has moved/rotated relative to its parent since the
        /// last poll (for boxes that ARE parented), or has meaningful raw
        /// Rigidbody velocity (for boxes with no parent).
        ///
        /// IMPORTANT: when a box is a Transform-child of something that itself
        /// rotates (e.g. a spinning tray), rb.linearVelocity / angularVelocity
        /// on the child get contaminated by the parent's motion � Unity forces
        /// the child transform to follow the parent every frame, and the
        /// physics engine reads that as the child moving even if it's
        /// perfectly still relative to the parent. So for parented boxes we
        /// skip the raw velocity check entirely and rely only on the
        /// parent-local position/rotation delta, which cancels out the
        /// parent's own motion automatically.
        /// </summary>
        private bool IsBoxMoving(Box box)
        {
            Transform parent = box.transform.parent;

            Vector3 localPos = parent.InverseTransformPoint(box.transform.position);
            Quaternion localRot = box.transform.localRotation;

            bool moved = false;

            if (lastLocalPositions.TryGetValue(box, out Vector3 prevPos))
                moved |= (localPos - prevPos).sqrMagnitude > positionDeltaThreshold;
            else
                moved = true; // first poll for this box, no baseline yet

            if (lastLocalRotations.TryGetValue(box, out Quaternion prevRot))
                moved |= Quaternion.Angle(localRot, prevRot) > rotationDeltaThreshold;

            lastLocalPositions[box] = localPos;
            lastLocalRotations[box] = localRot;

            return moved;
        }

        private void UpdateBallsUI() // UI
        {
            ballsRemainingText.text = maxBalls.ToString();
        }

        public void IncreaseForce()
        {
            forceIncBtnClicked = true;

            AudioManager.Instance.Play("Click");

            if (CoinsManager.Instance.collectedCoins >= 250)
            {
                AudioManager.Instance.Play("Reward");
                CoinsManager.Instance.LessCoins(250);
            }
            else
            {
                Toast.Instance.ShowToast("Not enough coins");
                return;
            }

            force += 10;
            SecurePlayerPrefs.SetFloat("Force", force);
        }
    }
}