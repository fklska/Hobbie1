using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
[Tool]
public partial class CoastLineStep : GenerationStep
{
    private List<TileType> sand_tilesToSearchFor = new List<TileType>()
    {
        TileType.Grass,
    };

    private List<TileType> sand_tilesAdjestedTo = new List<TileType>()
    {
        TileType.Water,
    };

    [Export] public Color preRenderColor;
    public override void Execute(GeneratorData genData)
    {
        if (Enabled)
        {
            HashSet<Vector2I> coastLine = GenerationUtils.GetEdgeTiles(sand_tilesToSearchFor, sand_tilesAdjestedTo, genData);
            genData.SandTilesCoords = coastLine;

            foreach (Vector2I sand_coord in coastLine)
            {
                genData.LandMapTiles[sand_coord.X, sand_coord.Y] = TileType.Sand;
                genData.LandTilesCoords.Remove(sand_coord);
            }
        }
    }
}
