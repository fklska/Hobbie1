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
    [Export] public Gradient grad;
    public Image map_render = new Image();

    public Image heat_render = new Image();
    public TextureRect MapTexture, HeatMap;

    [Export] public TileMapLayer grassTileMapTemplate;


    [Export] public GradientTexture2D heatGrad;
    [Export] public FastNoiseLite fractalHeatNoise; // Radom to latitudeGrad
    [Export] public Curve ClimateHeightCurve; // Curve coefs for highter values less temperature
    
    [Export] public Gradient debugLatitudeMask;
    [Export] public TextureRect DebugLatitude;
    [Export] public TextureRect DebugFractalMultiplier;
    [Export] public TextureRect DebugLatFractalMask;
    [Export] public float FractalStrech;

    public override void _Ready()
    {
        base._Ready();
        MapTexture = GetNode<TextureRect>("HeightMap");
        HeatMap = GetNode<TextureRect>("HeatMap");
        genData.ResetData();
        foreach (var step in steps)
        {
            step.Execute(genData);

        }

        preRender(genData);
        HeatMapRender(genData);
        // GenerateScene(genData);
    }

    public TileMapLayer GetMapFromTile(TileType tileType)
    {
        return tileType switch
        {
            TileType.Grass => grassTileMapTemplate,
            TileType.None => null,
            _ => throw new NotImplementedException()
        };
    }

    public int GetAtlasFromTile(TileType tileType)
    {
        return tileType switch
        {
            TileType.Grass => 0,
            TileType.Water => 3,
            TileType.Sand => 4,
            TileType.None => 15,
            _ => throw new NotImplementedException()
        };
    }

    public void ClearTileMapTemlate()
    {
        grassTileMapTemplate.Clear();
    }

    public void preRender(GeneratorData genData)
    {
        map_render = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);
        Godot.Color color = new Godot.Color();

        for (int x = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0; y < genData.mapSize.Y; y++)
            {
                color = grad.Sample(genData.LandMapHeights[x, y]);
                map_render.SetPixel(x, y, color);
            }
        }

        foreach (Vector2I sand_coord in genData.SandTilesCoords)
        {
            map_render.SetPixel(sand_coord.X, sand_coord.Y, genData.sandPreRenderColor);
        }

        MapTexture.Texture = ImageTexture.CreateFromImage(map_render);
    }

    public void HeatMapRender(GeneratorData genData)
    {
        fractalHeatNoise.Seed = genData.seed;
        heatGrad.Width = genData.mapSize.X;
        heatGrad.Height = genData.mapSize.Y;
        Image heatMask = heatGrad.GetImage();
        heat_render = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);

        // DEBUG
        Image DebugLatMask = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);
        Image DebugFractalReder = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);
        Image DebugLatReder = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);
        for (int x = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0; y < genData.mapSize.Y; y++)
            {
                float fractalValue = (fractalHeatNoise.GetNoise2D(x, y) + 1) / 2;
                float latitudeMultiplier = (heatMask.GetPixel(x, y).R * (fractalValue) + heatMask.GetPixel(x, y).R) * FractalStrech; // 
                float heatValue = latitudeMultiplier * (1 - genData.LandMapHeights[x, y] * ClimateHeightCurve.Sample(genData.LandMapHeights[x, y]));

                genData.HeatMapValues[x, y] = heatValue;
                heat_render.SetPixel(x, y, genData.GetHeatColor(heatValue));

                // DEBUG
                DebugLatMask.SetPixel(x, y, debugLatitudeMask.Sample(heatMask.GetPixel(x, y).R));
                DebugFractalReder.SetPixel(x, y, debugLatitudeMask.Sample(fractalValue));
                DebugLatReder.SetPixel(x, y, debugLatitudeMask.Sample(latitudeMultiplier));
            }
        }
        HeatMap.Texture = ImageTexture.CreateFromImage(heat_render);
        // DEBUG
        DebugLatitude.Texture = ImageTexture.CreateFromImage(DebugLatMask);
        DebugFractalMultiplier.Texture = ImageTexture.CreateFromImage(DebugFractalReder);
        DebugLatFractalMask.Texture = ImageTexture.CreateFromImage(DebugLatReder);
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
                grassTileMapTemplate.SetCell(new Vector2I(x, y), GetAtlasFromTile(genData.LandMapTiles[x, y]), new Vector2I(2, 1), 0);
            }
        }

        TileMapLayer grassMap = (TileMapLayer)grassTileMapTemplate.Duplicate();

        rootNode.AddChild(grassMap);
        grassMap.Owner = rootNode;

        PackedScene.Pack(rootNode);

        ResourceSaver.Save(PackedScene, "res://SavedWorlds/saved_scene.tscn");
    }
}
