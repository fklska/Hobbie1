using Godot;
using System;

[Tool]
[GlobalClass]
public partial class GeneratorData : Resource
{
    [Export] public Vector2I mapSize;
    public TileType[,] LandMapTiles;
    public TileType[,] ResourseMapTiles;
    public int seed = GenerationUtils.rnd.RandiRange(0, 2 >> 31);


    public void ResetData()
    {
        seed = GenerationUtils.rnd.RandiRange(0, 2 >> 31);
        LandMapTiles = new TileType[mapSize.X, mapSize.Y];
        ResourseMapTiles = new TileType[mapSize.X, mapSize.Y];
    }
    public enum TileType
    {
        None,
        Water,
        Sand,
        Grass

    }
    //public GeneratorData() { }
}
