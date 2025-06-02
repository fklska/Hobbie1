using Godot;
using System;

public static partial class GenerationUtils
{
    public static RandomNumberGenerator rnd = new RandomNumberGenerator();
    public static float[,] GenerateNoiseMap(FastNoiseLite noise, int width, int height)
    {
        float[,] noiseMap = new float[width, height];
        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                noiseMap[x, y] = (noise.GetNoise2D(x, y) + 1) / 2;
            }
        }
        return noiseMap;
    }

    public static void PrintNoiseMapArray(float[,] noiseMap)
    {
        int rows = noiseMap.GetLength(0);
        int cols = noiseMap.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write(noiseMap[i, j].ToString("0.00").PadLeft(6) + " ");
            }
            Console.WriteLine();
        }
    }
}
