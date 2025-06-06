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
    public TextureRect MapTexture;

    [Export] public TileMapLayer grassTileMapTemplate;
    [Export] public TileMapLayer waterTileMapTemplate;
    [Export] public TileMapLayer sandTileMapTemplate;

    public override void _Ready()
    {
        base._Ready();
        MapTexture = GetNode<TextureRect>("TextureRect");
        genData.ResetData();
        foreach (var step in steps)
        {
            step.Execute(genData);
        }

        preRender(genData);
        GenerateScene(genData);
    }

    public TileMapLayer GetMapFromTile(TileType tileType)
    {
        return tileType switch
        {
            TileType.Grass => grassTileMapTemplate,
            TileType.Water => waterTileMapTemplate,
            TileType.Sand => sandTileMapTemplate,
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
        waterTileMapTemplate.Clear();
        sandTileMapTemplate.Clear();
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
                //GetMapFromTile(genData.LandMapTiles[x, y]).SetCell(new Vector2I(x, y), 0, new Vector2I(2, 1), 0);
            }
        }

        TileMapLayer grassMap = (TileMapLayer)grassTileMapTemplate.Duplicate();
        //TileMapLayer sandMap = (TileMapLayer)sandTileMapTemplate.Duplicate();
        //TileMapLayer waterMap = (TileMapLayer)waterTileMapTemplate.Duplicate();

        rootNode.AddChild(grassMap);
        grassMap.Owner = rootNode;

        //rootNode.AddChild(sandMap);
        //sandMap.Owner = rootNode;

        //rootNode.AddChild(waterMap);
        //waterMap.Owner = rootNode;

        PackedScene.Pack(rootNode);

        ResourceSaver.Save(PackedScene, "res://SavedWorlds/saved_scene.tscn");
    }
}
