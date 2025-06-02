using Godot;
using System;

[Tool]
[GlobalClass]
public partial class BasicLandScapeStep : GenerationStep
{
    public override void Execute(GeneratorData generationData)
    {
        noise.Seed = generationData.seed;
        float[,] LandData = null;

        LandData = GenerationUtils.GenerateNoiseMap(noise, generationData.mapSize.X, generationData.mapSize.Y);

        GenerationUtils.PrintNoiseMapArray(LandData);
    }
}
