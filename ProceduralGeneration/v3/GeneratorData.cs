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

    public float[,] HeightMapValues;
    public float[,] HeatMapValues;
    public float[,] MoistureMapValues;

    [Export] public int seed = GenerationUtils.rnd.RandiRange(0, 1 << 31);

    public Image HeightMap, HeatMap, MoistureMap;

    [ExportCategory("HeightMap")]
    [Export] public Gradient HeightGrad;

    [ExportCategory("HeatMap")]
    [Export] public Gradient HeatMapRenderGradient;

    //Debug Render
    public Image DebugLatitudeMask;
    public Image DebugHeatFractal;
    public Image DebugLatFractal;

    [ExportCategory("MoistureMap")]
    [Export] public Gradient MoistureMapRenderColors;
    //Debug
    public Image DebugMoistureFractal;

    [Export] public bool NewSeed;

    public void ResetData()
    {
        if (NewSeed)
        { seed = GenerationUtils.rnd.RandiRange(0, 1 << 31); }
        LandMapTiles = new TileType[mapSize.X, mapSize.Y];

        HeightMapValues = new float[mapSize.X, mapSize.Y];
        HeatMapValues = new float[mapSize.X, mapSize.Y];
        MoistureMapValues = new float[mapSize.X, mapSize.Y];

        HeightMap = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        HeatMap = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        MoistureMap = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);

        DebugLatitudeMask = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        DebugHeatFractal = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        DebugLatFractal = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);

        DebugMoistureFractal = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
    }

    public void DefaultFill()
    {
        for (int x = 0; x < mapSize.X; x++)
        {
            for (int y = 0; y < mapSize.Y; y++)
            {
                LandMapTiles[x, y] = TileType.None;
            }
        }
    }
}

public enum TileType
{
    None,
    IceWater,
    Swamp,
    TropicWater,
    Snow,
    StoneMountain,
    Tundra,
    Taiga,
    RegularForest,
    TropicalForest,
    Savanna,
    Desert
}

public struct NoiseData
{
    public float[,] noiseValues;
    public float min;
    public float max;
}
