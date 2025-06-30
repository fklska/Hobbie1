using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
[GlobalClass]
public partial class EnviromnetGenerationStep : GenerationStep
{
    [Export] public FastNoiseLite TreeNoise;

    public HashSet<TileType> validTileTypes = new HashSet<TileType>()
    {
        TileType.TropicalForest,
        TileType.Swamp,
        TileType.Snow,
        TileType.RegularForest,
        TileType.Taiga,
    };
    public override void Execute(GeneratorData genData)
    {
        NoiseData TreeNoiseData = GenerationUtils.GetNoiseData(TreeNoise, genData.mapSize);


        for (int x = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0; y < genData.mapSize.Y; y++)
            {
                float treeNoiseValue = (TreeNoiseData.noiseValues[x, y] - TreeNoiseData.min) / (TreeNoiseData.max - TreeNoiseData.min);

                float value = treeNoiseValue;

                if (value > 0.95f && validTileTypes.Contains(genData.Map[x, y].Type))
                {
                    genData.Map[x, y].Resourse = ResorseType.Wood;
                }
            }
        }
    }
}
