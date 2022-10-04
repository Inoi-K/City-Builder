using UnityEngine;

[CreateAssetMenu(fileName = "Map", menuName = "ScriptableObjects/Map")]
public class Map : ScriptableObject {
    public Coord size;
    public Terrain[] terrains;
    public GameObject[] buildingPrefabs;

    public Vector2 buildingHeightMinMax;
    [Range(0, 1)] public float occupiedPercent;
    [Range(0, 1)] public float outlinePercent;
    public float tileSize;
}
