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
    [Export] public Gradient HeatMapRenderGradient;
    [Export] public float FractalStrech;


    [ExportCategory("MoistureMap")]
    [Export] public Curve MoistureHeightCurve;
    [Export] public FastNoiseLite fractalMoistureNoise;
    [Export] public Gradient MoistureColors;



    public override void Execute(GeneratorData generationData)
    {
        if (Enabled)
        {
            HeightNoise.Seed = generationData.seed;
            fractalHeatNoise.Seed = generationData.seed;
            fractalMoistureNoise.Seed = generationData.seed;

            Image LatitudeHeatGradient = LatitudeMask.GetImage();
            for (int x = 0; x < generationData.mapSize.X; x++)
            {
                for (int y = 0; y < generationData.mapSize.Y; y++)
                {
                    float heightValue = (HeightNoise.GetNoise2D(x, y) + 1) / 2;

                    float fractalHeatValue = (fractalHeatNoise.GetNoise2D(x, y) + 1) / 2;
                    float latitudeMultiplier = LatitudeHeatGradient.GetPixel(x, y).R * (1 + fractalHeatValue) * FractalStrech;
                    float heatValue = latitudeMultiplier * (1 - heightValue * ClimateHeightCurve.Sample(heightValue));

                    float fractalMoistureValue = (fractalMoistureNoise.GetNoise2D(x, y) + 1) / 2;
                    float moistureValue = (1 - heightValue * MoistureHeightCurve.Sample(heightValue) * (1 + fractalMoistureValue));

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
}
