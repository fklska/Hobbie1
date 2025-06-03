using Godot;
using System;
using System.Collections.Generic;

public static partial class GenerationUtils
{
    public static RandomNumberGenerator rnd = new RandomNumberGenerator();
    public static readonly List<Vector2I> dirs = new List<Vector2I>
    {
        new Vector2I(1, 0), // right
        new Vector2I(-1, 0), // left
        new Vector2I(0, 1), // down in GD
        new Vector2I(0, -1), // up in gd
        new Vector2I(1, 1), // right down
        new Vector2I(-1, 1), // lef down
        new Vector2I(1, -1), // right top
        new Vector2I(-1, -1), // left top

    };

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

    public static HashSet<Vector2I> GetEdgeTiles(List<TileType> typesToSearchFor, List<TileType> typesAdjestedTo, GeneratorData genData)
    {
        HashSet<Vector2I> borderLine = new HashSet<Vector2I>();

        for(int x  = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0;y < genData.mapSize.Y; y++)
            {
                if (typesToSearchFor.Contains(genData.LandMapTiles[x, y]) && IsAdjacentToTiles(x, y, typesAdjestedTo, genData.LandMapTiles))
                {
                    borderLine.Add(new Vector2I(x, y));
                }
            }
        }

        return borderLine;
    }

    public static bool IsAdjacentToTiles(int x, int y, List<TileType> tileTypes, TileType[,] LandmapTiles)
    {
        foreach (Vector2I direction in dirs)
        {
            int nx = x + direction.X;
            int ny = y + direction.Y;

            if (nx >= 0 && nx < LandmapTiles.GetLength(0) && ny >= 0 && ny < LandmapTiles.GetLength(1))
            {
                if (tileTypes.Contains(LandmapTiles[nx, ny]))
                {
                    return true;
                }
            }
        }

        return false;
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
