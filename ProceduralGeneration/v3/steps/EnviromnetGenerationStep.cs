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

            for (int x = 0; x < genData.mapSize.X; x++)
            {
                for (int y = 0; y < genData.mapSize.Y; y++)
                {
                    float treeNoiseValue = (TreeNoiseData.noiseValues[x, y] - TreeNoiseData.min) / (TreeNoiseData.max - TreeNoiseData.min);

                    float oreNoiseValue = (OreNoiseData.noiseValues[x, y] - OreNoiseData.min) / (OreNoiseData.max - OreNoiseData.min);

                    if (TreeValidTileTypes.Contains(genData.Map[x][y].Type))
                    {
                        if(!GenerationUtils.IsEdgeTile(x, y, genData.Map[x][y].Type, genData))
                        { genData.Map[x][y].SetResorsesValues(treeNoiseValue, oreNoiseValue, GetWoodType(treeNoiseValue, genData.Map[x][y].Type)); }
                    }

                    /*if (OreValidTileTypes.Contains(genData.Map[x][y].Type))
                    {
                        if (genData.Map[x][y].Resourse == ResorseType.None)
                        {
                            genData.Map[x][y].Resourse = GetOreResType(oreNoiseValue);
                        }
                    }*/
                }
            }
        }
    }

    public ResorseType GetWoodType(float value, TileType biome)
    {
        if (value < 0.1f) // SmallWood
        {
            if (biome != TileType.Desert)
            {
                if(biome == TileType.Snow) return ResorseType.SmallSnowWood;
            }
            return ResorseType.SmallWood;
        }

        if (value > 0.5f && value < 0.65f) // MediumWood
        {
            if (biome == TileType.Snow)
            {
                return ResorseType.MediumSnowWood;
            }
            return ResorseType.MediumWood;

        }

        if (value > 0.95f) // GiantWood
        {
            if (biome == TileType.Snow) return ResorseType.GiantSnowWood;

            if (biome == TileType.Desert) return ResorseType.PalmWood;
            
            return ResorseType.GiantWood;
        }

        return ResorseType.None;
    }

    public ResorseType GetOreResType(float value)
    {
        if (value < 0.1f)
        {
            return ResorseType.Gold;
        }

        if (value > 0.2f && value < 0.25f)
        {
            return ResorseType.Iron;
        }

        if (value > 0.63f && value < 0.7f)
        {
            return ResorseType.Stone;
        }
        return ResorseType.None;
    }
}
