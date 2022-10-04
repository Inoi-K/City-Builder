using UnityEngine;

[System.Serializable]
public class GameData {
    public Tile[,] tileMap;
    public float outlinePercent;
    public float tileSize;
    public Building[] buildings;
    
    public GameData(Tile[,] _tileMap, float _outlinePercent, float _tileSize, Building[] _buildings) {
        tileMap = _tileMap;
        outlinePercent = _outlinePercent;
        tileSize = _tileSize;
        buildings = _buildings;
    }
}

[System.Serializable]
public struct Building {
    public int sizeIndex;
    public float height;
    public float[] position;

    public Building(Transform building) {
        sizeIndex = Mathf.RoundToInt(building.localScale.x) - 1;
        height = building.localScale.y;
        position = new[] { building.position.x, building.position.y, building.position.z };
    }
}