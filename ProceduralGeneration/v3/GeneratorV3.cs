using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

[Tool]
public partial class GeneratorV3 : Node2D
{
    [Export] public GeneratorData genData;
    [Export] public Godot.Collections.Array<GenerationStep> steps;
    [Export] public TileMapLayer MainTileMap;

    [Export] public TextureRect FinalMap;
    [Export] public TextureRect HeightMap, HeatMap, MoistureMap;
    [Export] public TextureRect DebugLatitudeMask, DebugHeatFractal, DebugLatFractalMask;
    [Export] public TextureRect DebugMoistureFractal;

    public override void _Ready()
    {
        base._Ready();
        genData.ResetData();
        foreach (var step in steps)
        {
            step.Execute(genData);
        }

        preRender(genData);
        //TileMapRender(genData);
    }

    public int GetAtlasFromTile(TileType tileType)
    {
        return tileType switch
        {
            TileType.None => 15,
            _ => throw new NotImplementedException()
        };
    }

    public void ClearTileMapTemlate()
    {
        MainTileMap.Clear();
    }

    public void preRender(GeneratorData genData)
    {
        HeightMap.Texture = ImageTexture.CreateFromImage(genData.HeightMap);
        HeatMap.Texture = ImageTexture.CreateFromImage(genData.HeatMap);
        MoistureMap.Texture = ImageTexture.CreateFromImage(genData.MoistureMap);

        DebugLatitudeMask.Texture = ImageTexture.CreateFromImage(genData.DebugLatitudeMask);
        DebugHeatFractal.Texture = ImageTexture.CreateFromImage(genData.DebugHeatFractal);
        DebugLatFractalMask.Texture = ImageTexture.CreateFromImage(genData.DebugLatFractal);

        DebugMoistureFractal.Texture = ImageTexture.CreateFromImage(genData.DebugMoistureFractal);

        Image finalRender = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);
        for (int x = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0; y < genData.mapSize.Y; y++)
            {
                finalRender.SetPixel(x, y, GenerationUtils.getTileTypeColor(genData.LandMapTiles[x, y]));
            }
        }
        FinalMap.Texture = ImageTexture.CreateFromImage(finalRender);
    }

    public void TileMapRender(GeneratorData genData)
    {
        ClearTileMapTemlate();
        for (int x = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0; y < genData.mapSize.Y; y++)
            {
                MainTileMap.SetCell(new Vector2I(x, y), GenerationUtils.getTileTypeAtlas(genData.LandMapTiles[x, y]), new Vector2I(2, 1), 0);
            }
        }
    }

    public void GenerateScene(GeneratorData genData)
    {
        ClearTileMapTemlate();

        var PackedScene = new PackedScene();
        Node2D rootNode = new Node2D();

        for (int x = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0; y < genData.mapSize.Y; y++)
            {
                MainTileMap.SetCell(new Vector2I(x, y), GetAtlasFromTile(genData.LandMapTiles[x, y]), new Vector2I(2, 1), 0);
            }
        }

        TileMapLayer grassMap = (TileMapLayer)MainTileMap.Duplicate();

        rootNode.AddChild(grassMap);
        grassMap.Owner = rootNode;

        PackedScene.Pack(rootNode);

        ResourceSaver.Save(PackedScene, "res://SavedWorlds/saved_scene.tscn");
    }
}
