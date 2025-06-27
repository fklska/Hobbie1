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
        TileMapRender(genData);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        GetCell();
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

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("LeftMouseButton"))
        {
            Vector2I coords = MainTileMap.LocalToMap(GetGlobalMousePosition());
            float heightValue = genData.HeightMapValues[coords.X, coords.Y];
            float heatValue = genData.HeatMapValues[coords.X, coords.Y];
            float moistureValue = genData.MoistureMapValues[coords.X, coords.Y];
            GD.Print(String.Format("Coords: {0};\nHeight: {1};\nHeat: {2};\nMoisture: {3};\n", [coords, heightValue, heatValue, moistureValue]));
        }

        if (@event.IsActionPressed("DEBUG"))
        {
            GD.Print("WORK");
            foreach (var step in steps)
            {
                step.Execute(genData);
            }
            _Ready();
        }

    }

    [Export] public bool DebugInfo;
    public Vector2I lastcell = Vector2I.Zero;
    public void GetCell()
    {
        Vector2 mouseCoor = GetGlobalMousePosition();
        Vector2I cell = new Vector2I((int)mouseCoor.X, (int)mouseCoor.Y);
        if (cell < genData.mapSize && DebugInfo && lastcell != cell)
        {
            lastcell = cell;
            float height = genData.HeightMapValues[cell.X, cell.Y];
            float heat = genData.HeatMapValues[cell.X, cell.Y];
            float moisture = genData.MoistureMapValues[cell.X, cell.Y];
            TileType tileType = genData.LandMapTiles[cell.X, cell.Y];
            GD.Print(String.Format("Coords: {0};\nHeight: {1};\nHeat: {2};\nMoisture: {3};\nBiome: {4};\n", [cell, height, heat, moisture, tileType]));
        }
    }
}
