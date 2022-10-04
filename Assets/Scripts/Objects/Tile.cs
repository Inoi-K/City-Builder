[System.Serializable]
public class Tile {
    public Coord coord;
    public TerrainType type;
    public bool isVacant;

    public Tile(Coord _coord, TerrainType _type, bool _isVacant) {
        coord = _coord;
        type = _type;
        isVacant = _isVacant;
    }
}
