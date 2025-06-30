using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
[Tool]

public partial class CoastLineStep : GenerationStep
{
    [Export] public int CoastLineSize;

    private List<TileType> sand_tilesToSearchFor = new List<TileType>()
    {
        TileType.TropicalForest,
    };

    private List<TileType> sand_tilesAdjestedTo = new List<TileType>()
    {
        TileType.TropicWater,
    };

    private List<TileType> ValidTilesToExpand = new List<TileType>()
    {
        TileType.TropicWater,
        TileType.TropicalForest,
        TileType.Taiga,
        TileType.Savanna,
        TileType.Swamp,
    };

    [Export] public Color preRenderColor;
    public override void Execute(GeneratorData genData)
    {
        if (Enabled)
        {
            HashSet<Vector2I> coastLine = GenerationUtils.GetEdgeTiles(sand_tilesToSearchFor, sand_tilesAdjestedTo, genData);

            HashSet<Vector2I> sandTiles = GenerationUtils.ExpandEdgeTiles(coastLine, CoastLineSize, ValidTilesToExpand, genData);

            foreach (Vector2I sand_coord in sandTiles)
            {
                genData.Map[sand_coord.X, sand_coord.Y].Type = TileType.Desert;
            }
        }
    }
}
