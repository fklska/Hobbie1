using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
[Tool]
public partial class CoastLineStep : GenerationStep
{
    private List<TileType> sand_tilesToSearchFor = new List<TileType>()
    {
        TileType.RegularForest,
    };

    private List<TileType> sand_tilesAdjestedTo = new List<TileType>()
    {
        TileType.RegularWater,
    };

    [Export] public Color preRenderColor;
    public override void Execute(GeneratorData genData)
    {
        if (Enabled)
        {
            HashSet<Vector2I> coastLine = GenerationUtils.GetEdgeTiles(sand_tilesToSearchFor, sand_tilesAdjestedTo, genData);

            foreach (Vector2I sand_coord in coastLine)
            {
                genData.LandMapTiles[sand_coord.X, sand_coord.Y] = TileType.Desert;
            }
        }
    }
}
