using Godot;
using System;

public static partial class GenerationUtils
{
    public static RandomNumberGenerator rnd = new RandomNumberGenerator();
    public static float[,] GenerateNoiseMap(FastNoiseLite noise, int width, int height)
    {
        float[,] noiseMap = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                noiseMap[x, y] = (noise.GetNoise2D(x, y) + 1) / 2;
            }
        }
        return noiseMap;
    }

    public static void Print2DArray<T>(T[,] array)
    {
        int rows = array.GetLength(0);
        int cols = array.GetLength(1);

        Console.WriteLine("NEW ARRAY ===================== \n");
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write(array[i, j]?.ToString().PadLeft(3) + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("END ARRAY ===================== \n");
    }
}
