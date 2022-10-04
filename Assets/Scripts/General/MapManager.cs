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
        seed = Random.Range(0, 1000); // Set random seed to create a new map
        GenerateMap();
    }

    public void GenerateMap() {
        // Initialization
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
        int spawnedTerrainCount = 0;
        for (int i = 0; i < map.terrains.Length; ++i) {
            int currentTerrainCount = Mathf.RoundToInt(mapArea * map.terrains[i].percent);
            spawnedTerrainCount += currentTerrainCount;

            for (int j = 0; j < currentTerrainCount; ++j)
                SpawnTerrainAtTile(GetRandomTile(), map.terrains[i]);
        }
        // Additional terrain spawn on remaining tiles (for odd mapArea)
        for (int i = 0; i < mapArea - spawnedTerrainCount; ++i)
            SpawnTerrainAtTile(GetRandomTile(), map.terrains[prng.Next((int) TerrainType.Grass, (int) TerrainType.Sand)]);

        // Spawn buildings
        List<Tile> allVacantTiles = new List<Tile>(allTiles);
        shuffledVacantTiles = new Queue<Tile>(Utility.ShuffleArray(allVacantTiles.ToArray(), seed));
        int unoccupiedTilesLeft = Mathf.FloorToInt(mapArea * map.occupiedPercent);
        while (unoccupiedTilesLeft > 0) {
            int randomBuildingIndex = prng.Next(0, Mathf.FloorToInt(Mathf.Sqrt(unoccupiedTilesLeft)));
            if (Build(randomBuildingIndex, TileToPosition(GetRandomVacantTile())))
                unoccupiedTilesLeft -= (randomBuildingIndex + 1) * (randomBuildingIndex + 1);
        }
    }
    
    // Placing a terrain in a scene and configure map state
    void SpawnTerrainAtTile(Tile tile, Terrain terrain) {
        CreateTerrain(tile, terrain);

        // Configure tile info
        tile.type = terrain.type;
        SetVacant(tile, terrain.type is TerrainType.Grass or TerrainType.Sand);
    }

    // Place a terrain in a scene
    Transform CreateTerrain(Tile tile, Terrain terrain) {
        Vector3 position = TileToPosition(tile);
        Transform newTerrain = Instantiate(terrain.prefab, position, Quaternion.identity, mapHolder).transform;
        newTerrain.localScale = Vector3.one * (1 - currentOutlinePercent) * currentTileSize;

        return newTerrain;
    }

    Terrain GetTerrainByType(TerrainType type) {
        return map.terrains[(int) type];
    }

    #region Building
    public bool Build(int sizeIndex, Vector3 leftDownPosition) {
        Tile currentTile = GetTileFromPosition(leftDownPosition);

        if (!currentTile.isVacant)
            return false;

        // Neighbours in up-right direction (from left-down tile)
        Tile[] neighbours = GetNeighbours(currentTile, sizeIndex);
        if (neighbours == null || !neighbours.All(x => x.isVacant))
            return false;

        // Spawn building
        float height = Mathf.Lerp(map.buildingHeightMinMax.x, map.buildingHeightMinMax.y, (float) prng.NextDouble());
        Vector3 spawnPosition = leftDownPosition;
        spawnPosition += GetDirectionToCenter(sizeIndex, height);
        CreateBuilding(sizeIndex, height, spawnPosition);

        // Update map state
        foreach (Tile neighbour in neighbours)
            SetVacant(neighbour, false);

        return true;
    }

    // Calculating direction to the center of a building (for a spawn position)
    public Vector3 GetDirectionToCenter(int sizeIndex, float height) {
        Vector3 direction = Vector3.one * .5f * currentTileSize;
        direction.x *= sizeIndex; 
        direction.z *= sizeIndex;
        direction.y += height;

        return direction;
    }
    
    // Place a building in a scene
    public Transform CreateBuilding(int sizeIndex, float height, Vector3 position) {
        Transform newBuilding = Instantiate(map.buildingPrefabs[sizeIndex], position, Quaternion.identity, buildingHolder).transform;
        Vector3 correctionScale = new Vector3((1 - currentOutlinePercent) * currentTileSize, height, (1 - currentOutlinePercent) * currentTileSize);
        newBuilding.localScale = Vector3.Scale(newBuilding.localScale, correctionScale);
        
        return newBuilding;
    }

    // Destroy a building and set tiles under it to vacant state
    public void DestroyBuilding(Transform building) {
        int sizeIndex = Mathf.RoundToInt(building.localScale.x) - 1;
        Vector3 leftDownPosition = building.position - GetDirectionToCenter(sizeIndex, building.localScale.y);
        Tile currentTile = GetTileFromPosition(leftDownPosition);

        // Update map state
        foreach (Tile neighbour in GetNeighbours(currentTile, sizeIndex))
            SetVacant(neighbour, true);

        // Destroy the building object
        Destroy(building.gameObject);
    }

    // Gather neighbour tiles in up-right direction
    Tile[] GetNeighbours(Tile tile, int sizeIndex) {
        int size = sizeIndex + 1;
        int count = size * size;
        Tile[] neighbours = new Tile[count];
        Coord origin = tile.coord;

        for (int x = 0; x < size; ++x) {
            for (int y = 0; y < size; ++y) {
                int neighbourX = origin.x + x;
                int neighbourY = origin.y + y;
                
                // Bounds check
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

        // Can only dry swamp or water terrains
        if (tile.type is not (TerrainType.Swamp or TerrainType.Water))
            return;
        
        
        tile.type -= 1; // Update type of the tile
        SpawnTerrainAtTile(tile, GetTerrainByType(tile.type)); // Place in a scene new terrain object
        Destroy(currentTerrain); // Destroy the old terrain object
    }
    #endregion

    // Load saved city
    public void LoadCity(GameData data) {
        MapHolderHandler();
        BuildingHolderHandler();
        
        tileMap = data.tileMap;
        currentOutlinePercent = data.outlinePercent;
        currentTileSize = data.tileSize;

        // Place terrain
        for (int x = 0; x < tileMap.GetLength(0); ++x) {
            for (int y = 0; y < tileMap.GetLength(1); ++y) {
                Tile tile = tileMap[x, y];
                CreateTerrain(tile, GetTerrainByType(tile.type));
            }
        }

        // Place buildings
        foreach (Building building in data.buildings) {
            Vector3 position = new Vector3(building.position[0], building.position[1], building.position[2]);
            CreateBuilding(building.sizeIndex, building.height, position);
        }
    }
    
    public Tile[,] GetTileMap() {
        return tileMap;
    }

    public Building[] GetBuildings() {
        Building[] buildings = new Building[buildingHolder.childCount];

        for (int i = 0; i < buildingHolder.childCount; ++i)
            buildings[i] = new Building(buildingHolder.GetChild(i));

        return buildings;
    }

    void SetVacant(Tile tile, bool value) {
        tile.isVacant = value;
    }
    
    void MapHolderHandler() {
        const string mapHolderName = "Generated Map";
        if (transform.Find(mapHolderName)) // if the parent already exists
            DestroyImmediate(transform.Find(mapHolderName).gameObject);
        mapHolder = new GameObject(mapHolderName).transform;
        mapHolder.parent = transform;
    }

    void BuildingHolderHandler() {
        const string buildingHolderName = "Buildings";
        if (transform.Find(buildingHolderName)) // if the parent already exists
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
        x = Mathf.Clamp (x, 0, tileMap.GetLength (0) - 1);
        y = Mathf.Clamp (y, 0, tileMap.GetLength (1) - 1);
        return tileMap[x, y];
    }
}
