using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapManager : MonoBehaviour {
    public Map map;
    [SerializeField] int seed;

    Queue<Tile> shuffledTiles;
    Queue<Tile> shuffledVacantTiles;
    Tile[,] tileMap;
    float currentOutlinePercent;
    float currentTileSize;

    Transform mapHolder;
    Transform buildingHolder;

    System.Random prng;

    void Start() {
        GenerateMap();
    }

    public void GenerateMap() {
        prng = new System.Random(seed);
        tileMap = new Tile[map.size.x, map.size.y];
        currentOutlinePercent = map.outlinePercent;
        currentTileSize = map.tileSize;
        int mapArea = map.size.x * map.size.y;

        // Create a holder for a map
        MapHolderHandler();

        // Create a holder for buildings
        BuildingHolderHandler();

        // Create tiles and shuffle them
        Tile[] allTiles = new Tile[mapArea];
        for (int x = 0; x < map.size.x; ++x) {
            for (int y = 0; y < map.size.y; ++y) {
                tileMap[x, y] = new Tile(new Coord(x, y), TerrainType.Grass, true);
                allTiles[x * map.size.y + y] = tileMap[x, y];
            }
        }
        shuffledTiles = new Queue<Tile>(Utility.ShuffleArray(allTiles, seed));

        // Spawn terrain
        List<Tile> allVacantTiles = new List<Tile>(allTiles);
        int spawnedTerrainCount = 0;
        for (int i = 0; i < map.terrains.Length; ++i) {
            int currentTerrainCount = Mathf.RoundToInt(mapArea * map.terrains[i].percent);
            spawnedTerrainCount += currentTerrainCount;

            for (int j = 0; j < currentTerrainCount; ++j)
                SpawnTerrainAtRandom(map.terrains[i]);
        }
        // Additional terrain spawn on remaining tiles (for odd mapArea)
        for (int i = 0; i < mapArea - spawnedTerrainCount; ++i)
            SpawnTerrainAtRandom(map.terrains[prng.Next((int)TerrainType.Grass, (int)TerrainType.Sand)]);

        // Spawn buildings
        shuffledVacantTiles = new Queue<Tile>(Utility.ShuffleArray(allVacantTiles.ToArray(), seed));
        int unoccupiedTilesLeft = Mathf.FloorToInt(mapArea * map.occupiedPercent);
        while (unoccupiedTilesLeft > 0) {
            int randomBuildingIndex = prng.Next(0, Mathf.FloorToInt(Mathf.Sqrt(unoccupiedTilesLeft)));
            if (Build(randomBuildingIndex, TileToPosition(GetRandomVacantTile())))
                unoccupiedTilesLeft -= (randomBuildingIndex + 1) * (randomBuildingIndex + 1);
        }
    }

    void SpawnTerrainAtTile(Terrain terrain, Tile tile) {
        CreateTerrain(terrain, tile);

        // Configure tile info
        tileMap[tile.coord.x, tile.coord.y].type = terrain.type;
        SetVacant(tileMap[tile.coord.x, tile.coord.y], terrain.type is TerrainType.Grass or TerrainType.Sand);
    }

    void CreateTerrain(Terrain terrain, Tile tile) {
        Vector3 position = TileToPosition(tile);
        Transform newTerrain = Instantiate(terrain.prefab, position, Quaternion.identity, mapHolder).transform;
        newTerrain.localScale = Vector3.one * (1 - currentOutlinePercent) * currentTileSize;
    }

    void SpawnTerrainAtRandom(Terrain terrain) {
        SpawnTerrainAtTile(terrain, GetRandomTile());
    }

    Terrain GetTerrainByType(TerrainType type) {
        return map.terrains[(int)type];
    }

    #region Building
    public bool Build(int sizeIndex, Vector3 position) {
        Tile currentTile = GetTileFromPosition(position);

        if (!currentTile.isVacant)
            return false;

        // Neighbours in up-right direction
        Tile[] neighbours = GetNeighbours(currentTile, sizeIndex + 1);
        if (neighbours == null || !neighbours.All(x => x.isVacant))
            return false;

        CreateBuilding(sizeIndex, position);

        foreach (Tile neighbour in neighbours)
            SetVacant(neighbour, false);

        return true;
    }

    public Transform CreateBuilding(int sizeIndex, Vector3 position) {
        float height = Mathf.Lerp(map.buildingHeightMinMax.x, map.buildingHeightMinMax.y, (float)prng.NextDouble());
        Vector3 spawnPosition = Vector3.one * .5f * currentTileSize;
        spawnPosition.x *= sizeIndex;
        spawnPosition.z *= sizeIndex;
        spawnPosition.y += height;
        spawnPosition += position;

        Transform newBuilding = Instantiate(map.buildingPrefabs[sizeIndex], spawnPosition, Quaternion.identity, buildingHolder).transform;
        Vector3 correctionScale = new Vector3((1 - currentOutlinePercent) * currentTileSize, height, (1 - currentOutlinePercent) * currentTileSize);
        newBuilding.localScale = Vector3.Scale(newBuilding.localScale, correctionScale);

        return newBuilding;
    }

    Tile[] GetNeighbours(Tile tile, int size) {
        int count = size * size;
        Tile[] neighbours = new Tile[count];
        Coord origin = tile.coord;

        for (int x = 0; x < size; ++x) {
            for (int y = 0; y < size; ++y) {
                int neighbourX = origin.x + x;
                int neighbourY = origin.y + y;

                if (neighbourX >= map.size.x || neighbourY >= map.size.y)
                    return null;

                neighbours[x * size + y] = tileMap[neighbourX, neighbourY];
            }
        }

        return neighbours;
    }
    #endregion

    #region MakeTerrain
    public void DryTerrain(GameObject currentTerrain) {
        Tile tile = GetTileFromPosition(currentTerrain.transform.position);

        if (tile.type is not (TerrainType.Swamp or TerrainType.Water))
            return;

        tile.type -= 1;
        SpawnTerrainAtTile(GetTerrainByType(tile.type), tile);
        Destroy(currentTerrain);
    }
    #endregion

    void SetVacant(Tile tile, bool value) {
        tile.isVacant = value;
    }

    public void SetVacant(Vector3 position, bool value) {
        SetVacant(GetTileFromPosition(position), value);
    }

    public void LoadCity(GameData data) {
        MapHolderHandler();
        BuildingHolderHandler();

        tileMap = data.tileMap;
        currentOutlinePercent = data.outlinePercent;
        currentTileSize = data.tileSize;

        for (int x = 0; x < tileMap.GetLength(0); ++x) {
            for (int y = 0; y < tileMap.GetLength(1); ++y) {
                Tile tile = tileMap[x, y];
                CreateTerrain(, tile);
            }
        }
    }

    public Tile[,] GetTileMap() {
        return tileMap;
    }

    public Building[] GetBuildings() {
        Building[] buildings = new Building[buildingHolder.childCount];

        for (int i = 0; i < buildingHolder.childCount; ++i)
            buildings[i] = new Building(buildingHolder.GetChild(0));

        return buildings;
    }

    void MapHolderHandler() {
        const string mapHolderName = "Generated Map";
        if (transform.Find(mapHolderName)) // if a parent already exists
            DestroyImmediate(transform.Find(mapHolderName).gameObject);
        mapHolder = new GameObject(mapHolderName).transform;
        mapHolder.parent = transform;
    }

    void BuildingHolderHandler() {
        const string buildingHolderName = "Buildings";
        if (transform.Find(buildingHolderName)) // if a parent already exists
            DestroyImmediate(transform.Find(buildingHolderName).gameObject);
        buildingHolder = new GameObject(buildingHolderName).transform;
        buildingHolder.parent = transform;
    }

    Vector3 TileToPosition(Tile tile) {
        return new Vector3(-map.size.x / 2f + .5f + tile.coord.x, 0, -map.size.y / 2f + .5f + tile.coord.y) * currentTileSize;
    }

    Tile GetRandomTile() {
        Tile randomTile = shuffledTiles.Dequeue();
        shuffledTiles.Enqueue(randomTile);
        return randomTile;
    }

    Tile GetRandomVacantTile() {
        Tile randomVacantTile = shuffledVacantTiles.Dequeue();
        shuffledVacantTiles.Enqueue(randomVacantTile);
        return randomVacantTile;
    }

    Tile GetTileFromPosition(Vector3 position) {
        int x = Mathf.RoundToInt(position.x / currentTileSize + (map.size.x - 1) / 2f);
        int y = Mathf.RoundToInt(position.z / currentTileSize + (map.size.y - 1) / 2f);
        x = Mathf.Clamp(x, 0, tileMap.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, tileMap.GetLength(1) - 1);
        return tileMap[x, y];
    }
}
