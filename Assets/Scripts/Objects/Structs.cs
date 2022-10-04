using UnityEngine;

[System.Serializable]
public struct Terrain {
    public GameObject prefab;
    public TerrainType type;
    [Range(0, 1)]
    public float percent;
}

public enum TerrainType {
    Grass,
    Sand,
    Swamp,
    Water,
}

[System.Serializable]
public struct Coord {
    public int x;
    public int y;

    public Coord(int _x, int _y) {
        x = _x;
        y = _y;
    }
}
