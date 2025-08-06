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
        TileType.DeepWater,
        TileType.TropicalForest
    };

    [Export] public Color preRenderColor;
    public override void Execute(GeneratorData genData)
    {
        if (Enabled)
        {
            for (int chunk_x = 0; chunk_x < GenerationSettings.MAP_CHUNK_SIZE_X; chunk_x++)
            {
                for (int chunk_y = 0; chunk_y < GenerationSettings.MAP_CHUNK_SIZE_Y; chunk_y++)
                {
                    ChunkData chunk = genData.ChunkMap[chunk_x][chunk_y];

                    HashSet<Vector2I> coastLine = GenerationUtils.GetEdgeTiles(sand_tilesToSearchFor, sand_tilesAdjestedTo, chunk);

                    HashSet<Vector2I> sandTiles = GenerationUtils.ExpandEdgeTiles(coastLine, CoastLineSize, ValidTilesToExpand, chunk);

                    foreach (Vector2I sand_coord in sandTiles)
                    {
                        chunk.Map[sand_coord.X][sand_coord.Y].Type = TileType.Desert;
                    }
                        
                }
            }
        }
    }
}
