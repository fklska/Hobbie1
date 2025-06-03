using Godot;
using System;

[Tool]
[GlobalClass]
public partial class BasicLandScapeStep : GenerationStep
{
    [Export(PropertyHint.Range, "0, 1")] float waterThreshold;
    [Export(PropertyHint.Range, "0, 1")] float landThreshold;


    public override void Execute(GeneratorData generationData)
    {
        if (Enabled)
        {
            noise.Seed = generationData.seed;
            float[,] LandData = null;

            LandData = GenerationUtils.GenerateNoiseMap(noise, generationData.mapSize.X, generationData.mapSize.Y);

            generationData.LandMapHeights = LandData;
            for (int x = 0; x < generationData.mapSize.X; x++)
            {
                for (int y = 0; y < generationData.mapSize.Y; y++)
                {
                    if (LandData[x, y] < waterThreshold)
                    {
                        generationData.LandMapTiles[x, y] = TileType.Water;
                        generationData.WaterTilesCoords.Add(new Vector2I(x, y));
                    }
                    else
                    {
                        generationData.LandMapTiles[x, y] = TileType.Grass;
                        generationData.LandTilesCoords.Add(new Vector2I(x, y));
                    }
                }
            }


            //GenerationUtils.Print2DArray(generationData.LandMapTiles);
        }
    }
}
