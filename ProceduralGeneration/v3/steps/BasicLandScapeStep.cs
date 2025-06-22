using Godot;
using System;

[Tool]
[GlobalClass]
public partial class BasicLandScapeStep : GenerationStep
{
    [ExportCategory("HeightMap")]
    [Export] public FastNoiseLite HeightNoise;

    [ExportCategory("HeatMap")]
    [Export] public GradientTexture2D LatitudeMask;
    [Export] public FastNoiseLite fractalHeatNoise; // Radom to latitudeGrad
    [Export] public Curve ClimateHeightCurve; // Curve coefs for highter values less temperature
    [Export] public float HeatFractalStrech;


    [ExportCategory("MoistureMap")]
    [Export] public Curve MoistureHeightCurve;
    [Export] public FastNoiseLite fractalMoistureNoise;
    [Export] public float MoistureFractalStrech;



    public override void Execute(GeneratorData generationData)
    {
        if (Enabled)
        {
            HeightNoise.Seed = generationData.seed;
            fractalHeatNoise.Seed = generationData.seed;
            fractalMoistureNoise.Seed = generationData.seed;
            LatitudeMask.Height = generationData.mapSize.Y;
            LatitudeMask.Width = generationData.mapSize.X;


            Image LatitudeHeatGradient = LatitudeMask.GetImage();
            for (int x = 0; x < generationData.mapSize.X; x++)
            {
                for (int y = 0; y < generationData.mapSize.Y; y++)
                {
                    float heightValue = (HeightNoise.GetNoise2D(x, y) + 1) / 2;

                    float fractalHeatValue = (fractalHeatNoise.GetNoise2D(x, y) + 1) / 2;
                    float latitudeMultiplier = LatitudeHeatGradient.GetPixel(x, y).R * (1 + fractalHeatValue) * HeatFractalStrech;
                    float heatValue = latitudeMultiplier * (1 - heightValue * ClimateHeightCurve.Sample(heightValue));

                    float fractalMoistureValue = (fractalMoistureNoise.GetNoise2D(x, y) + 1) / 2;
                    float moistureValue = (1 - heightValue * MoistureHeightCurve.Sample(heightValue) * (1 + fractalMoistureValue)) / MoistureFractalStrech;

                    // Determine TileType (Biome)
                    generationData.LandMapTiles[x, y] = getTileType(heightValue, heatValue, moistureValue);

                    // Store Data
                    generationData.HeightMapValues[x, y] = heightValue;
                    generationData.HeatMapValues[x, y] = heatValue;
                    generationData.MoistureMapValues[x, y] = moistureValue;

                    // Render DELETE AFTER PROMO
                    generationData.HeightMap.SetPixel(x, y, generationData.HeightGrad.Sample(heightValue));
                    generationData.HeatMap.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(heatValue));
                    generationData.MoistureMap.SetPixel(x, y, generationData.MoistureMapRenderColors.Sample(moistureValue));

                    generationData.DebugLatitudeMask.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(LatitudeHeatGradient.GetPixel(x, y).R));
                    generationData.DebugHeatFractal.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(fractalHeatValue));
                    generationData.DebugLatFractal.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(latitudeMultiplier));


                    generationData.DebugMoistureFractal.SetPixel(x, y, generationData.MoistureMapRenderColors.Sample(fractalMoistureValue));
                }
            }
        }
    }

    public TileType getTileType(float heightValue, float heatValue, float moistureValue)
    {
        if (moistureValue < 0.25f) // Dry
        {
            if (heatValue < 0.3f)
            {
                if (heightValue < 0.65f)
                {
                    return TileType.Tundra;
                }
                else
                {
                    return TileType.StoneMountain;
                }
            }
            else if (heatValue < 0.5f)
            {
                return TileType.Savanna;
            }
            else
            {
                return TileType.Desert;
            }
        }
        else if (moistureValue < 0.7f) // Medium
        {
            if (heatValue < 0.3f)
            {
                if (heightValue < 0.65f)
                {
                    return TileType.Tundra;
                }
                else
                {
                    return TileType.SnowMountain;
                }
            }
            else if (heatValue < 0.5f)
            {
                return TileType.Taiga;
            }
            else if (heatValue < 0.7f)
            {
                return TileType.RegularForest;
            }
            else
            {
                return TileType.TropicalForest;
            }
        }
        else // Wet
        {
            if (heatValue < 0.3f)
            {
                return TileType.IceWater;
            }
            else if (heatValue < 0.7f)
            {
                return TileType.RegularWater;
            }
            else
            {
                return TileType.TropicWater;
            }
        }
    }
}
