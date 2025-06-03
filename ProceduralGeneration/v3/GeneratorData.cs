using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class GeneratorData : Resource
{
    [Export] public Vector2I mapSize;
    public TileType[,] LandMapTiles;
    public float[,] LandMapHeights;
    public HashSet<Vector2I> LandTilesCoords;
    public HashSet<Vector2I> WaterTilesCoords;

    public HashSet<Vector2I> SandTilesCoords;
    [Export] public Color sandPreRenderColor;

    public TileType[,] ResourseMapTiles;
    public HashSet<Vector2I> ResorseTilesCoords;

    public int seed = GenerationUtils.rnd.RandiRange(0, 1 << 31);


    public void ResetData()
    {
        seed = GenerationUtils.rnd.RandiRange(0, 1 << 31);
        LandMapTiles = new TileType[mapSize.X, mapSize.Y];
        ResourseMapTiles = new TileType[mapSize.X, mapSize.Y];
        LandTilesCoords = new();
        WaterTilesCoords = new();
        ResorseTilesCoords = new();
        SandTilesCoords = new();
        LandMapHeights = null;
    }
    //public GeneratorData() { }
}

public enum TileType
{
    None,
    Water,
    Sand,
    Grass
}
