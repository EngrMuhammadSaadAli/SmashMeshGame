using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Which axis a size-variant prefab is elongated along (e.g. a "3_X_2" prefab is stretched along X).</summary>
public enum SizeAxis { X, Y, Z }

namespace NextLevelGames
{
    /// <summary>
    /// Reads a level .json (produced by the level editor tool) and spawns the
    /// tables, objects (stacked boxes/cans), and blockers it describes.
    ///
    /// JSON shape (see LevelData.cs):
    ///   LevelData -> stages[] -> { objects[], tables[], blockers[] }
    ///
    /// - Each TableData describes one table/platform (position, rotation, size,
    ///   and optional movement/rotation behavior driven by TableController).
    /// - Each ObjectData describes one spawned piece, positioned LOCALLY
    ///   relative to the table it belongs to (via tableId), not in world space.
    /// - Each BlockerData describes an obstacle.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("The single table prefab used for every table entry in the json.")]
        public GameObject tablePrefab;

        [Header("Object Prefabs")]
        [Tooltip("Drop ALL object prefabs here. Name each prefab 'Type' for the base 1x1x1 size (e.g. '12') " +
                 "or 'Type_Axis_Size' for elongated variants (e.g. '3_X_2', '12_Y_3'). Type, axis, and size " +
                 "are parsed automatically from the name - no per-prefab setup needed.")]
        public List<GameObject> objectPrefabs = new();

        [Tooltip("Blocker prefabs, indexed by BlockerData.type once real blocker data exists.")]
        public GameObject[] blockerPrefabs;

        [Header("Behavior")]
        // How many spawned objects are still alive. Counts down as boxes are
        // destroyed (see BoxDestroyed) and triggers the win condition at 0.
        private int spawnedBoxesCount;

        // The parsed level currently loaded, exposed read-only so other
        // scripts (UI, win/lose checks, etc.) can inspect it if needed.
        public LevelData CurrentLevel { get; private set; }

        // tableId (from the json) -> the Transform of the table we spawned for it.
        // Objects use this to find and parent themselves under their owning table.
        private readonly Dictionary<int, Transform> _spawnedTables = new();

        // Lookup key (built from a prefab's parsed type/axis/size, see
        // MakeSizedPrefabKey) -> the matching prefab. Rebuilt every level load
        // in case objectPrefabs changes at runtime.
        private readonly Dictionary<string, GameObject> _sizedPrefabLookup = new();

        /// <summary>
        /// Entry point: loads Levels/Level{levelNumber}.json from Resources and
        /// spawns the requested stage (most levels only have one stage, index 0).
        /// </summary>
        public void GenerateLevel(int levelNumber, int stageIndex = 0)
        {
            // Level json files live under a Resources/Levels folder so they can
            // be loaded by name at runtime without needing direct references.
            TextAsset levelJson = Resources.Load<TextAsset>("Levels/Level" + levelNumber);

            if (levelJson == null)
            {
                LogErr("JSON file not found at " + "Levels/Level" + levelNumber);
                return;
            }

            // JsonUtility can deserialize straight into Vector3/Quaternion because
            // those types' public field names (x,y,z / x,y,z,w) match the json exactly.
            CurrentLevel = JsonUtility.FromJson<LevelData>(levelJson.text);

            if (CurrentLevel?.stages == null || stageIndex < 0 || stageIndex >= CurrentLevel.stages.Count)
            {
                LogErr($"[LevelGenerator] Level {levelNumber} has no stage {stageIndex}.");
                return;
            }

            // Wipe any previously spawned level before building the new one.
            //ClearSpawned();
            spawnedBoxesCount = 0;

            // Re-scan objectPrefabs and build the type/axis/size -> prefab
            // lookup fresh each time, in case prefabs were added/changed.
            BuildSizedPrefabLookup();

            // moveCount from the json becomes the player's ball budget for this level.
            FindFirstObjectByType<CannonShooter>().maxBalls = CurrentLevel.moveCount;

            StageData stage = CurrentLevel.stages[stageIndex];

            // Tables need to know how "deep" (Z) to render before they're spawned,
            // since depth is derived from where their objects sit, not from json
            // scale data. Compute that first, then spawn tables, then objects
            // (objects need their table to already exist so they can parent to it).
            Dictionary<int, float> zScaleByTable = ComputeTableZScales(stage.objects);
            SpawnTables(stage.tables, zScaleByTable);
            SpawnObjects(stage.objects);
            SpawnBlockers(stage.blockers);
        }

        /// <summary>
        /// Spawns one GameObject per TableData entry. Tables are placed at
        /// their json X/Z position with a forced Y of 0 (the prefab's own
        /// pivot/height defines vertical placement), and rotated per json.
        /// </summary>
        private void SpawnTables(List<TableData> tables, Dictionary<int, float> zScaleByTable)
        {
            if (tables == null) return;

            foreach (TableData t in tables)
            {
                GameObject go = Instantiate(tablePrefab, new Vector3(t.pos.x, 0, t.pos.z), t.rot);

                // The visible table surface is a child named "TableTop" so it can
                // be scaled independently of the table's base/legs. X scale comes
                // from the json's dim.x (table width); Z scale is NOT taken from
                // json — it's computed to exactly fit however many rows of objects
                // are actually stacked on this table (see ComputeTableZScales).
                Transform tableTop = go.transform.Find("TableTop");
                if (tableTop != null)
                {
                    float zScale = zScaleByTable.TryGetValue(t.id, out float z) ? z : 1f;
                    tableTop.localScale = new Vector3(t.dim.x, 1, zScale);
                }

                // TableController reads doRot/movH/movV etc. from TableData and
                // animates the table at runtime (moving platforms, spinners, etc).
                TableController controller = go.GetComponent<TableController>() ?? go.AddComponent<TableController>();
                controller.Init(t);

                // Remember this table's Transform so SpawnObjects can parent
                // objects under the correct table via their tableId.
                _spawnedTables[t.id] = go.transform;
            }
        }

        /// <summary>
        /// Figures out how deep (along Z) each table's surface needs to be so it
        /// visually fits its stacked objects, without relying on any scale value
        /// from the json (which doesn't reliably describe this).
        ///
        /// For each table, this finds the spread between the lowest and highest
        /// local Z position among its objects, then adds 1. Examples (object
        /// spacing is always 1 unit, so this maps directly to "row count"):
        ///   objects only at z=0            -> spread 0   -> scale 1
        ///   objects at z=-0.5 and z=0.5     -> spread 1   -> scale 2
        ///   objects at z=-1, 0, and 1        -> spread 2   -> scale 3
        ///
        /// Z values are rounded to the nearest 0.5 first because exported json
        /// often contains tiny floating point noise (e.g. 1.49e-7 instead of a
        /// clean 0) from how the level editor serializes transforms.
        /// </summary>
        private Dictionary<int, float> ComputeTableZScales(List<ObjectData> objects)
        {
            var result = new Dictionary<int, float>();
            if (objects == null) return result;

            var minZ = new Dictionary<int, float>();
            var maxZ = new Dictionary<int, float>();

            foreach (ObjectData o in objects)
            {
                float z = Mathf.Round(o.pos.z * 2f) / 2f; // snap to nearest 0.5, kills float noise
                if (!minZ.TryGetValue(o.tableId, out float curMin) || z < curMin) minZ[o.tableId] = z;
                if (!maxZ.TryGetValue(o.tableId, out float curMax) || z > curMax) maxZ[o.tableId] = z;
            }

            foreach (var kvp in minZ)
            {
                int tableId = kvp.Key;
                float span = maxZ[tableId] - kvp.Value;
                result[tableId] = Mathf.Round(span) + 1f;
            }

            return result;
        }

        /// <summary>
        /// Spawns one GameObject per ObjectData entry, resolving which prefab to
        /// use from its type + size (see TryGetSizedPrefab), and parenting it
        /// under its owning table using LOCAL position/rotation — the json's
        /// pos/rot values are relative to the table, not world space, so a
        /// table that's moved/rotated correctly carries its objects along.
        /// </summary>
        private void SpawnObjects(List<ObjectData> objects)
        {
            if (objects == null) return;

            foreach (ObjectData o in objects)
            {
                bool foundSizedPrefab = TryGetSizedPrefab(o.type, o.size, out GameObject prefab);

                if (!foundSizedPrefab)
                {
                    // No exact (type, axis, size) prefab exists for this object.
                    // Fall back to the plain base prefab for this type (named
                    // just "12", "3", etc) and let it get manually scaled below,
                    // rather than skipping the object entirely.
                    string expectedName = DescribeExpectedPrefabName(o.type, o.size);
                    LogErr($"[LevelGenerator] Missing sized prefab '{expectedName}' (type {o.type}, size {o.size}). " +
                                      $"Falling back to a simple/base prefab named '{o.type}' instead.");
                    prefab = FindPrefabByName(o.type.ToString());
                }

                if (prefab == null)
                {
                    LogErr($"[LevelGenerator] No prefab found for type {o.type} with size {o.size}.");
                    continue;
                }

                // Parent under the table this object belongs to, so it moves
                // with that table if the table animates. Objects with a
                // tableId that doesn't match any spawned table fall back to
                // sitting directly under this LevelGenerator.
                Transform parent = _spawnedTables.TryGetValue(o.tableId, out Transform tableTf) ? tableTf : transform;

                GameObject go = Instantiate(prefab, parent);
                go.transform.localPosition = o.pos;
                go.transform.localRotation = o.rot;

                // A matched sized prefab (e.g. "3_X_2") is already modeled at its
                // correct dimensions, so it must NOT be rescaled — doing so would
                // double the sizing. Only the generic fallback prefab (which is
                // just a plain 1x1x1 shape) needs to be manually scaled to o.size.
                go.transform.localScale = foundSizedPrefab ? Vector3.one : o.size;
                spawnedBoxesCount++;
            }
        }

        /// <summary>Finds a prefab in objectPrefabs by exact GameObject name match.</summary>
        private GameObject FindPrefabByName(string typeName)
        {
            if (objectPrefabs == null) return null;
            return objectPrefabs.Find(p => p != null && p.name == typeName);
        }

        /// <summary>
        /// Builds the lookup by parsing each prefab's own name, so nothing needs to be typed
        /// into the inspector manually. Expected name formats:
        ///   "12"        -> base prefab, type 12
        ///   "3_X_2"     -> type 3, axis X, size 2
        /// Prefabs whose names don't match either pattern are ignored here (they can still be
        /// used elsewhere, e.g. blockers/tables) and a warning is logged so typos are easy to spot.
        /// </summary>
        private void BuildSizedPrefabLookup()
        {
            _sizedPrefabLookup.Clear();

            if (objectPrefabs == null) return;

            foreach (GameObject prefab in objectPrefabs)
            {
                if (prefab == null) continue;

                // Split "3_X_2" into ["3","X","2"], or "12" into ["12"].
                string[] parts = prefab.name.Split('_');
                string key;

                if (parts.Length == 1)
                {
                    // Single-part name -> this is a base (1x1x1) prefab for that type.
                    if (!int.TryParse(parts[0], out int type))
                    {
                        LogWarn($"[LevelGenerator] Prefab '{prefab.name}' doesn't match 'Type' naming. Skipping.");
                        continue;
                    }
                    key = MakeSizedPrefabKey(type, SizeAxis.X, 1, true);
                }
                else if (parts.Length == 3)
                {
                    // Three-part name -> an elongated variant: Type_Axis_Size.
                    if (!int.TryParse(parts[0], out int type) ||
                        !Enum.TryParse(parts[1], true, out SizeAxis axis) ||
                        !int.TryParse(parts[2], out int magnitude))
                    {
                        LogWarn($"[LevelGenerator] Prefab '{prefab.name}' doesn't match 'Type_Axis_Size' naming. Skipping.");
                        continue;
                    }
                    key = MakeSizedPrefabKey(type, axis, magnitude, false);
                }
                else
                {
                    // Anything else (wrong number of underscores) can't be parsed.
                    LogWarn($"[LevelGenerator] Prefab '{prefab.name}' doesn't match 'Type' or 'Type_Axis_Size' naming. Skipping.");
                    continue;
                }

                if (_sizedPrefabLookup.ContainsKey(key))
                {
                    // Two prefabs parsed to the same key — keep the first one found
                    // and flag the duplicate so it can be renamed/removed.
                    LogWarn($"[LevelGenerator] Duplicate prefab for key '{key}' (prefab '{prefab.name}'). Skipping.");
                    continue;
                }

                _sizedPrefabLookup[key] = prefab;
            }
        }

        /// <summary>
        /// Builds the prefab name we'd expect to exist for a given type/size,
        /// purely for use in error messages (so a missing-prefab warning tells
        /// you exactly what to name the prefab you need to add).
        /// </summary>
        private string DescribeExpectedPrefabName(int type, Vector3 size)
        {
            int xMag = Mathf.RoundToInt(size.x);
            int yMag = Mathf.RoundToInt(size.y);
            int zMag = Mathf.RoundToInt(size.z);
            int maxMag = Mathf.Max(xMag, yMag, zMag);

            if (maxMag <= 1) return type.ToString();

            SizeAxis axis = xMag == maxMag ? SizeAxis.X : (yMag == maxMag ? SizeAxis.Y : SizeAxis.Z);
            return $"{type}_{axis}_{maxMag}";
        }

        /// <summary>
        /// Given an object's type and its size vector from the json, figures out
        /// whether it needs the base (1x1x1) prefab or an elongated axis variant
        /// — decided from the SIZE, not the type — then looks it up in
        /// _sizedPrefabLookup. Returns false if no matching prefab was found.
        /// </summary>
        private bool TryGetSizedPrefab(int type, Vector3 size, out GameObject prefab)
        {
            int xMag = Mathf.RoundToInt(size.x);
            int yMag = Mathf.RoundToInt(size.y);
            int zMag = Mathf.RoundToInt(size.z);
            int maxMag = Mathf.Max(xMag, yMag, zMag);

            if (maxMag <= 1)
            {
                // Object isn't elongated in any direction -> use the base prefab.
                return _sizedPrefabLookup.TryGetValue(MakeSizedPrefabKey(type, SizeAxis.X, 1, true), out prefab);
            }

            // Elongated -> figure out which axis has the largest size component
            // (that's the direction the object is stretched in) and look up the
            // prefab for that exact (type, axis, magnitude) combination.
            SizeAxis axis = xMag == maxMag ? SizeAxis.X : (yMag == maxMag ? SizeAxis.Y : SizeAxis.Z);

            return _sizedPrefabLookup.TryGetValue(MakeSizedPrefabKey(type, axis, maxMag, false), out prefab);
        }

        /// <summary>
        /// Builds the dictionary key used by _sizedPrefabLookup. Base (1x1x1)
        /// prefabs use a fixed "{type}_base" key (axis doesn't matter for them);
        /// elongated variants use "{type}_{axis}_{magnitude}" so, e.g., type 3
        /// can have separate 2-unit and 3-unit variants along different axes
        /// without colliding with each other or with its own base prefab.
        /// </summary>
        private string MakeSizedPrefabKey(int type, SizeAxis axis, int magnitude, bool isBase)
        {
            return isBase ? $"{type}_base" : $"{type}_{axis}_{magnitude}";
        }

        /// <summary>
        /// Spawns one random blocker prefab per BlockerData entry. Blockers are
        /// instantiated at their PREFAB's own position/rotation (not from json —
        /// BlockerData currently has no confirmed pos/rot usage here), since the
        /// real blocker schema hasn't been finalized yet.
        /// </summary>
        private void SpawnBlockers(List<BlockerData> blockers)
        {
            if (blockers == null || blockers.Count == 0) return;

            if (blockerPrefabs == null || blockerPrefabs.Length == 0)
            {
                LogWarn("[LevelGenerator] Level has blockers but no blockerPrefabs are assigned.");
                return;
            }

            foreach (BlockerData b in blockers)
            {
                GameObject prefab = blockerPrefabs[UnityEngine.Random.Range(0, blockerPrefabs.Length)];
                Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            }
        }

        ///// <summary>Destroys everything previously spawned under this LevelGenerator and resets table tracking, ready for a fresh GenerateLevel call.</summary>
        //private void ClearSpawned()
        //{
        //    for (int i = transform.childCount - 1; i >= 0; i--)
        //    {
        //        Destroy(transform.GetChild(i).gameObject);
        //    }
        //    _spawnedTables.Clear();
        //}

        /// <summary>
        /// Called by a spawned object when it's destroyed (e.g. knocked off the
        /// table by a cannon ball). Decrements the remaining-box counter and
        /// triggers the win condition once every object has been cleared.
        /// </summary>
        public void BoxDestroyed()
        {
            if (spawnedBoxesCount <= 0) return;
            spawnedBoxesCount--;
            if (spawnedBoxesCount <= 0)
            {
                FindFirstObjectByType<GameManager>().Win();
            }
        }

        // Guards UnfreezeAllBoxes so it only ever runs its (relatively expensive)
        // FindObjectsByType scan once per level, even if called multiple times.
        [HideInInspector] public bool isBoxesUsingGravity;

        /// <summary>
        /// Removes physics constraints from every Rigidbody in the scene so
        /// spawned boxes start falling/reacting to physics normally. Boxes
        /// presumably spawn with constraints frozen so they hold their stacked
        /// position until gameplay explicitly releases them.
        /// </summary>
        public void UnfreezeAllBoxes(bool releaseRotation)
        {
            if (isBoxesUsingGravity) return;

            if(releaseRotation)
                isBoxesUsingGravity = true;

            foreach (Rigidbody rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (releaseRotation)
                {
                    rb.constraints = RigidbodyConstraints.None;
                }
                else
                {
                    rb.constraints &= ~(RigidbodyConstraints.FreezePositionX |
                    RigidbodyConstraints.FreezePositionY |
                    RigidbodyConstraints.FreezePositionZ);
                }
            }
        }

        // ---------- Editor-only logging ----------
        // Debug.Log/LogWarning/LogError calls throughout this script go through
        // these wrappers instead of being called directly. The #if UNITY_EDITOR
        // guard means these calls compile to nothing in real builds (standalone,
        // mobile, etc) — so level-data warnings/errors are visible while working
        // in the Editor but add zero overhead and produce no console spam for players.

        private static void LogWarn(string message)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#endif
        }

        private static void LogErr(string message)
        {
#if UNITY_EDITOR
            Debug.LogError(message);
#endif
        }
    }
}