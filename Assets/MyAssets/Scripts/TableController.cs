using UnityEngine;

namespace NextLevelGames
{
    /// <summary>
    /// Animates a spawned table at runtime using the movement/rotation fields
    /// carried on its TableData (doRot, movH, movV, etc — set by the level
    /// editor tool, not hardcoded here). Supports:
    ///   - Continuous Y rotation (doRot / rotSpd)
    ///   - Horizontal ping-pong movement along local X, between the ABSOLUTE
    ///     positions movHMin and movHMax (not an offset from spawn position)
    ///   - Vertical ping-pong movement along local Y, between the ABSOLUTE
    ///     positions movVMin and movVMax
    /// </summary>
    public class TableController : MonoBehaviour
    {
        public TableData Data { get; private set; }

        [Header("Safety Clamp")]
        [Tooltip("If enabled, tightens the table's horizontal ping-pong range to whichever is smaller: movHMin/movHMax (from the json) or [minX, maxX] here. All four are ABSOLUTE local X positions, not offsets.")]
        public bool clampXPos = false;
        public float minX = -2f;
        public float maxX = 2f;

        // The table's local position at the moment Init() was called. Kept
        // around for reference/other axes that aren't being driven by movH/movV.
        private Vector3 _startLocalPos;

        // Current absolute local X/Y position while ping-ponging, and which
        // direction (+1/-1) each is currently moving in.
        private float _hPos;
        private float _vPos;
        private int _hDir = 1;
        private int _vDir = 1;

        /// <summary>
        /// Called once right after this table is spawned/instantiated (see
        /// LevelGenerator.SpawnTables). Captures the starting position and
        /// normalizes the configured movement directions.
        /// </summary>
        public void Init(TableData data)
        {
            Data = data;
            _startLocalPos = transform.localPosition;

            // dirH/dirV of 0 would mean the table never moves even with movH/movV
            // enabled, which is almost certainly not intended — default to +1
            // in that case so movement always actually happens once enabled.
            _hDir = data.dirH == 0 ? 1 : data.dirH;
            _vDir = data.dirV == 0 ? 1 : data.dirV;

            // movHMin/movHMax/movVMin/movVMax are ABSOLUTE local X/Y positions
            // (not offsets from the table's spawn position) — the table
            // ping-pongs directly between those literal values. Start the
            // moving axis at its own current position, clamped into its
            // configured range, so it begins wherever the table actually
            // spawned rather than snapping straight to a bound on frame one.
            _hPos = Mathf.Clamp(_startLocalPos.x, data.movHMin, data.movHMax);
            _vPos = Mathf.Clamp(_startLocalPos.y, data.movVMin, data.movVMax);

            if (clampXPos && minX > maxX)
            {
                LogWarn($"[TableController] minX ({minX}) is greater than maxX ({maxX}) on '{name}' — swapping them.");
                (minX, maxX) = (maxX, minX);
            }
        }

        private void Update()
        {
            if (Data == null) return;

            // Continuous rotation around world-up, independent of any movH/movV movement.
            if (Data.doRot)
            {
                transform.Rotate(Vector3.up, Data.rotSpd * Time.deltaTime, Space.World);
            }

            // Nothing else to do if neither movement axis is enabled.
            if (!Data.movH && !Data.movV) return;

            Vector3 newLocalPos = transform.localPosition;

            if (Data.movH)
            {
                // If clampXPos is enabled, tighten the ping-pong bounds to the
                // intersection of movHMin/movHMax and minX/maxX, so _hPos
                // itself bounces cleanly at whichever limit is tighter — rather
                // than clamping the OUTPUT separately, which would freeze the
                // table's visible position while _hPos kept changing
                // underneath (causing a stop-then-sudden-resume stutter).
                float effectiveMin = Data.movHMin;
                float effectiveMax = Data.movHMax;

                if (clampXPos)
                {
                    effectiveMin = Mathf.Max(effectiveMin, minX);
                    effectiveMax = Mathf.Min(effectiveMax, maxX);
                }

                _hPos += Data.movSpdH * Time.deltaTime * _hDir;
                if (_hPos > effectiveMax || _hPos < effectiveMin)
                {
                    _hDir *= -1;
                    _hPos = Mathf.Clamp(_hPos, effectiveMin, effectiveMax);
                }
                newLocalPos.x = _hPos;
            }

            if (Data.movV)
            {
                // Same ping-pong logic as movH, but along the vertical (Y) axis.
                _vPos += Data.movSpdV * Time.deltaTime * _vDir;
                if (_vPos > Data.movVMax || _vPos < Data.movVMin)
                {
                    _vDir *= -1;
                    _vPos = Mathf.Clamp(_vPos, Data.movVMin, Data.movVMax);
                }
                newLocalPos.y = _vPos;
            }

            transform.localPosition = newLocalPos;
        }

        // ---------- Editor-only logging ----------
        // #if UNITY_EDITOR means this compiles to nothing in real builds, so it
        // costs nothing for players but still surfaces setup mistakes in-editor.
        private static void LogWarn(string message)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#endif
        }
    }
}