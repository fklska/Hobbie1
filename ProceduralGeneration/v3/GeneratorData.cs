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

    public float[,] HeatMapValues;
    [Export] public Color Coldest;
    [Export] public Color Cold;
    [Export] public Color Midlle;
    [Export] public Color Warmest;

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
        DefaultFill();
        ResourseMapTiles = new TileType[mapSize.X, mapSize.Y];
        LandTilesCoords = new();
        WaterTilesCoords = new();
        ResorseTilesCoords = new();
        SandTilesCoords = new();
        LandMapHeights = null;
        HeatMapValues = new float[mapSize.X, mapSize.Y];
    }
    //public GeneratorData() { }

    public void DefaultFill()
    {
        for (int x = 0; x < mapSize.X; x++)
        {
            for (int y = 0; y < mapSize.Y; y++)
            {
                LandMapTiles[x, y] = TileType.Water;
            }
        }
    }

    public Color GetHeatColor(float value)
    {
        if (value <= 0.2f)
        {
            return Coldest;
        }
        else if (value <= 0.4f)
        {
            return Cold;
        }
        else if (value <= 0.7f)
        {
            return Midlle;
        }
        else return Warmest;
    }
}

public enum TileType
{
    None,
    Water,
    Sand,
    Grass
}
