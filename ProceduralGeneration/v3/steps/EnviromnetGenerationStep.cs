using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

[Tool]
[GlobalClass]
public partial class EnviromnetGenerationStep : GenerationStep
{
    [Export] public FastNoiseLite TreeNoise;
    [Export] public FastNoiseLite OreNoise;

    public HashSet<TileType> TreeValidTileTypes = new HashSet<TileType>()
    {
        TileType.Snow,
        TileType.RegularForest,
        TileType.Taiga,
        TileType.TropicalForest,
    };
    public HashSet<TileType> OreValidTileTypes = new HashSet<TileType>()
    {
        TileType.Desert,
        TileType.Savanna,
        TileType.Tundra,
        TileType.Snow,
        TileType.Taiga,
    };
    public override void Execute(GeneratorData genData)
    {
        if (Enabled)
        {
            TreeNoise.Seed = genData.seed;
            OreNoise.Seed = genData.seed;

            NoiseData TreeNoiseData = GenerationUtils.GetNoiseData(TreeNoise, genData.mapSize);
            NoiseData OreNoiseData = GenerationUtils.GetNoiseData(OreNoise, genData.mapSize);

            for (int chunk_x = 0; chunk_x < GenerationSettings.MAP_CHUNK_SIZE_X; chunk_x++)
            {
                for (int chunk_y = 0; chunk_y < GenerationSettings.MAP_CHUNK_SIZE_Y; chunk_y++)
                {
                    ChunkData chunk = genData.ChunkMap[chunk_x][chunk_y];
                    int local_x = 0;

                    for (int x = chunk.rect.X; x < chunk.rect.Z; x++)
                    {
                        int local_y = 0;
                        for (int y = chunk.rect.Y; y < chunk.rect.W; y++)
                        {
                            float treeNoiseValue = (TreeNoiseData.noiseValues[x, y] - TreeNoiseData.min) / (TreeNoiseData.max - TreeNoiseData.min);

                            float oreNoiseValue = (OreNoiseData.noiseValues[x, y] - OreNoiseData.min) / (OreNoiseData.max - OreNoiseData.min);

                            if (TreeValidTileTypes.Contains(chunk.Map[local_x][local_y].Type))
                            {

                                if (chunk.Map[local_x][local_y].Type == TileType.TropicalForest) { treeNoiseValue *= 1.2f; }
                                if (!GenerationUtils.IsEdgeTile(local_x, local_y, chunk.Map[local_x][local_y].Type, chunk))
                                {
                                    chunk.Map[local_x][local_y].SetResorsesValues(treeNoiseValue, oreNoiseValue, GetWoodType(treeNoiseValue, chunk.Map[local_x][local_y].Type));
                                    //if (chunk.Map[local_x][local_y].Resourse != ResorseType.None) genData.TreeCoords.Add(new Vector2I(x * GenerationUtils.TILE_SIZE, y * GenerationUtils.TILE_SIZE));
                                }
                            }

                            if (OreValidTileTypes.Contains(chunk.Map[local_x][local_y].Type))
                            {
                                if (chunk.Map[local_x][local_y].Resourse == ResorseType.None)
                                {
                                    chunk.Map[local_x][local_y].Resourse = GetOreResType(oreNoiseValue);
                                }
                            }

                            local_y++;
                        }
                        local_x++;
                    }
                }
            }
        }
    }

    public ResorseType GetWoodType(float value, TileType biome)
    {
        if (value > 0.8f) // GiantWood
        {
            if (biome == TileType.Snow) return ResorseType.GiantSnowWood;

            if (biome == TileType.Desert) return ResorseType.PalmWood;

            if (biome == TileType.TropicalForest) return ResorseType.None;

            return ResorseType.GiantWood;
        }

        else if (value > 0.7f) // MediumWood
        {
            if (biome == TileType.Snow)
            {
                return ResorseType.MediumSnowWood;
            }

            if (biome == TileType.TropicalForest)
            {
                return ResorseType.TropicWood;
            }
            return ResorseType.MediumWood;

        }

        else if (value > 0.5f) // SmallWood
        {

            if(biome == TileType.Snow) return ResorseType.SmallSnowWood;
            if (biome == TileType.Desert) return ResorseType.None;
            if (biome == TileType.TropicalForest) return ResorseType.None;

            return ResorseType.SmallWood;
        }

        else return ResorseType.None;
    }

    public ResorseType GetOreResType(float value)
    {
        if (value > 0.9f || value < 0.1f)
        {
            return ResorseType.Gold;
        }

        else if (value > 0.8f)
        {
            return ResorseType.Iron;
        }

        else if (value > 0.7f)
        {
            return ResorseType.Stone;
        }

        else return ResorseType.None;
    }
}
