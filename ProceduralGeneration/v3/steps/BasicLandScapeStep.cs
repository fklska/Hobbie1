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

            NoiseData heightNoiseData = GenerationUtils.GetNoiseData(HeightNoise, generationData.mapSize);
            NoiseData heatNoiseData = GenerationUtils.GetNoiseData(fractalHeatNoise, generationData.mapSize);
            NoiseData moistureNoiseData = GenerationUtils.GetNoiseData(fractalMoistureNoise, generationData.mapSize);

            LatitudeMask.Height = generationData.mapSize.Y;
            LatitudeMask.Width = generationData.mapSize.X;

            Image LatitudeHeatGradient = LatitudeMask.GetImage();
            for (int chunk_x = 0; chunk_x < generationData.x_chunk_count; chunk_x++)
            {
                for (int chunk_y = 0; chunk_y < generationData.y_chunk_count; chunk_y++)
                {
                    ChunkData chunk = generationData.ChunkMap[chunk_x][chunk_y];
                    int local_x = 0;

                    for (int x = chunk.rect.X; x < chunk.rect.Z; x++)
                    {
                        int local_y = 0;
                        for (int y = chunk.rect.Y; y < chunk.rect.W; y++)
                        {
                            //GD.Print($"Coords: {x} {y}");

                            float heightValue = (heightNoiseData.noiseValues[x, y] - heightNoiseData.min) / (heightNoiseData.max - heightNoiseData.min);

                            float fractalHeatValue = (heatNoiseData.noiseValues[x, y] - heatNoiseData.min) / (heatNoiseData.max - heatNoiseData.min);
                            float latitudeMultiplier = LatitudeHeatGradient.GetPixel(x, y).R * (1 + fractalHeatValue);
                            float heatValue = latitudeMultiplier * (1 - heightValue * ClimateHeightCurve.Sample(heightValue));

                            float fractalMoistureValue = (moistureNoiseData.noiseValues[x, y] - moistureNoiseData.min) / (moistureNoiseData.max - moistureNoiseData.min);
                            float moistureValue = (1 - heightValue * MoistureHeightCurve.Sample(heightValue)) * (1 + fractalMoistureValue);

                            // Store Data
                            generationData.Map[x][y].UpdateInfo(heightValue, heatValue, moistureValue, getTileType(heightValue, heatValue, moistureValue));
                            
                            chunk.Map[local_x][local_y].UpdateInfo(heightValue, heatValue, moistureValue, getTileType(heightValue, heatValue, moistureValue));

                            // Render DELETE AFTER PROMO
                            generationData.HeightMap.SetPixel(x, y, generationData.HeightGrad.Sample(heightValue));
                            generationData.HeatMap.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(heatValue));
                            generationData.MoistureMap.SetPixel(x, y, generationData.MoistureMapRenderColors.Sample(moistureValue));
                            generationData.BiomeMap.SetPixel(x, y, GenerationUtils.getTileTypeColor(generationData.Map[x][y].Type));

                            generationData.DebugLatitudeMask.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(LatitudeHeatGradient.GetPixel(x, y).R));
                            generationData.DebugHeatFractal.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(fractalHeatValue));
                            generationData.DebugLatFractal.SetPixel(x, y, generationData.HeatMapRenderGradient.Sample(latitudeMultiplier));


                            generationData.DebugMoistureFractal.SetPixel(x, y, generationData.MoistureMapRenderColors.Sample(fractalMoistureValue));
                           
                            local_y++;
                        }
                        local_x++;
                    }
                }
            }
        }
    }

    public TileType getTileType(float heightValue, float heatValue, float moistureValue)
    {
        if (moistureValue < 0.35f) // Dry
        {
            if (heatValue < 0.3f)
            {
                return TileType.Tundra;
            }
            else if (heatValue < 0.45f)
            {
                return TileType.RegularForest;
            }
            else if (heatValue < 0.50f)
            {
                return TileType.Savanna;
            }
            else
            {
                return TileType.Desert;
            }
        }
        else if (moistureValue < 0.4f) // Medium
        {
            if (heatValue < 0.3f)
            {
                return TileType.Snow;
            }
            else if (heatValue < 0.45f)
            {
                return TileType.RegularForest;
            }
            else
            {
                return TileType.Savanna;
            }

        }
        else if (moistureValue < 0.7f) // Medium
        {
            if (heatValue < 0.3f)
            {
                return TileType.Snow;
            }
            else if (heatValue < 0.55f)
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
            if (moistureValue < 0.8f)
            { return TileType.TropicWater;}

            return TileType.DeepWater;
        }
    }
}
