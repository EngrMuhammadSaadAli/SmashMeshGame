using System;
using System.Collections.Generic;
using UnityEngine;

// Root object of a Level json file (e.g. Level1.json).
// NOTE: Vector3 / Quaternion are used directly (not custom structs) because
// JsonUtility can map JSON {x,y,z} / {x,y,z,w} straight onto Unity's built-in
// types, since their public field names match exactly.
[Serializable]
public class LevelData
{
    public int levelIndex;
    public int moveCount;
    public int difficulty;
    public List<StageData> stages;
}

[Serializable]
public class StageData
{
    public List<ObjectData> objects;
    public List<TableData> tables;
    public List<BlockerData> blockers;
}

// A single spawnable piece (e.g. a stacked cube) that belongs to a table.
[Serializable]
public class ObjectData
{
    public int tableId;   // which TableData.id this object is parented/attached to
    public int type;      // index into the object prefab array
    public Vector3 size;  // used as localScale
    public Vector3 pos;   // local/world position (see LevelGenerator note)
    public Quaternion rot;
}

// A table/platform. Also carries motion parameters (movH/movV/doRot) used
// by TableController to animate the table at runtime.
[Serializable]
public class TableData
{
    public int id;
    public Vector3 pos;
    public Quaternion rot;
    public Vector3 scl;
    public Vector3 dim;

    public bool doRot;
    public float rotSpd;

    public bool movH;
    public float movHMin;
    public float movHMax;
    public int dirH;
    public float movSpdH;

    public bool movV;
    public float movVMin;
    public float movVMax;
    public int dirV;
    public float movSpdV;
}

// Blocker schema is unknown — the sample JSON's "blockers" array is empty.
// This mirrors ObjectData's shape as a reasonable best guess; update the
// fields once you have real blocker data to test against.
[Serializable]
public class BlockerData
{
    public int id;
    public int type;
    public Vector3 size;
    public Vector3 pos;
    public Quaternion rot;
}
