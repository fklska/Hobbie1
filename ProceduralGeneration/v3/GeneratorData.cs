using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class GeneratorData : Resource
{
    [Export] public Vector2I mapSize = GenerationSettings.MapSize;
    [Export] public Vector2I SpawnPoint;
    [Export] public String WorldName = GenerationUtils.GenerateNameWorld();
    [Export] public Godot.Collections.Array<Godot.Collections.Array<Tile>> Map; // Delete
    [Export] public Godot.Collections.Array<Vector2I> TreeCoords; // Delete

    [Export] public Godot.Collections.Array<Godot.Collections.Array<ChunkData>> ChunkMap;

    [Export] public int seed;

    [Export] public Image HeightMap, HeatMap, MoistureMap, BiomeMap;

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
    public int x_chunk_count;
    public int y_chunk_count;
    public void ResetData()
    {

        mapSize = GenerationSettings.MapSize;
        x_chunk_count = (int)mapSize.X / GenerationSettings.CHUNK_SIZE;
        y_chunk_count = (int)mapSize.Y / GenerationSettings.CHUNK_SIZE;

        SpawnPoint = new Vector2I(mapSize.X * GenerationUtils.TILE_SIZE / 2, mapSize.Y * GenerationUtils.TILE_SIZE / 2);
        WorldName = GenerationUtils.GenerateNameWorld();
        //GenerateNewSeed();
        TreeCoords = new Godot.Collections.Array <Vector2I>();
        Map = InitializeArray();
        ChunkMap = InitializeChunkArray();

        BiomeMap = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        HeightMap = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        HeatMap = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        MoistureMap = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);

        DebugLatitudeMask = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        DebugHeatFractal = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
        DebugLatFractal = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);

        DebugMoistureFractal = Image.CreateEmpty(mapSize.X, mapSize.Y, false, Image.Format.Rgba8);
    }

    public Godot.Collections.Array<Godot.Collections.Array<Tile>> InitializeArray()
    {
        Godot.Collections.Array<Godot.Collections.Array<Tile>> Map = new Godot.Collections.Array<Godot.Collections.Array<Tile>>();
        Map.Resize(mapSize.X);
        for (int x = 0; x < mapSize.X; x++)
        {
            Map[x] = new Godot.Collections.Array<Tile>();
            Map[x].Resize(mapSize.Y);

            for (int y = 0; y < mapSize.Y; y++)
            {
                Map[x][y] = new Tile();
            }
        }
        return Map;
    }

    public Godot.Collections.Array<Godot.Collections.Array<ChunkData>> InitializeChunkArray()
    {
        Godot.Collections.Array<Godot.Collections.Array<ChunkData>> Map = new Godot.Collections.Array<Godot.Collections.Array<ChunkData>>();
        Map.Resize(x_chunk_count);

        for (int x = 0; x < x_chunk_count; x++)
        {
            Map[x] = new Godot.Collections.Array<ChunkData>();
            Map[x].Resize(y_chunk_count);

            for (int y = 0; y < y_chunk_count; y++)
            {
                Map[x][y] = new ChunkData();
                Map[x][y].GlobalCoord = new Vector2I(x, y);
                Map[x][y].ResetData();
            }
        }
        return Map;
    }

    public void GenerateNewSeed()
    {
        seed = GenerationUtils.rnd.RandiRange(0, 1 << 31);
    }

    public void DefaultFill()
    {
        for (int x = 0; x < mapSize.X; x++)
        {
            for (int y = 0; y < mapSize.Y; y++)
            {
                
            }
        }
    }

    public SimpleGeneratorData ToSimpleData()
    {
        SimpleGeneratorData data = new SimpleGeneratorData();
        data.seed = seed;
        data.BiomeMap = BiomeMap;
        data.WorldName = WorldName;
        data.mapSize = mapSize;
        data.SpawnPoint = SpawnPoint;
        data.fullDataPath = ResourcePath;
        return data;
    }
}

public enum TileType
{
    None,
    DeepWater,
    TropicWater,
    Snow,
    Tundra,
    Taiga,
    RegularForest,
    TropicalForest,
    Savanna,
    Desert
}

public enum ResorseType
{
    None,
    SmallWood,
    MediumWood,
    GiantWood,
    SmallSnowWood,
    MediumSnowWood,
    GiantSnowWood,
    PalmWood,
    Stone,
    Iron,
    Gold
}

public struct NoiseData
{
    public float[,] noiseValues;
    public float min;
    public float max;
}
