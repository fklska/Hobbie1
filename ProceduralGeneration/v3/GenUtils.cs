using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public const int TILE_SIZE = 64;

    public static HashSet<Vector2I> GetEdgeTiles(List<TileType> typesToSearchFor, List<TileType> typesAdjestedTo, GeneratorData genData)
    {
        HashSet<Vector2I> borderLine = new HashSet<Vector2I>();

        for(int x  = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0;y < genData.mapSize.Y; y++)
            {
                if (typesToSearchFor.Contains(genData.Map[x, y].Type) && IsAdjacentToTiles(x, y, typesAdjestedTo, genData.Map))
                {
                    borderLine.Add(new Vector2I(x, y));
                }
            }
        }

        return borderLine;
    }

    public static HashSet<Vector2I> ExpandEdgeTiles(HashSet<Vector2I> initialLayer, int widht, List<TileType> validTiles, GeneratorData genData)
    {
        HashSet<Vector2I> LastExpendedTiles = new HashSet<Vector2I>();
        HashSet<Vector2I> currentOutTileLine = new HashSet<Vector2I>(initialLayer);

        for (int i = 0; i < widht; i++)
        {
            foreach (Vector2I tileCoor in currentOutTileLine)
            {
                foreach (Vector2I dir in dirs)
                {
                    Vector2I nCoords = tileCoor + dir;
                    if (nCoords.X >= 0 && nCoords.X < genData.mapSize.X && nCoords.Y >= 0 && nCoords.Y < genData.mapSize.Y)
                    {
                        if (validTiles.Contains(genData.Map[nCoords.X, nCoords.Y].Type))
                        {
                            initialLayer.Add(nCoords);
                            LastExpendedTiles.Add(nCoords);
                        }
                    }
                }
            }
            currentOutTileLine = LastExpendedTiles;
            LastExpendedTiles = new HashSet<Vector2I>();
        }
        return initialLayer;
    }

    public static bool IsAdjacentToTiles(int x, int y, List<TileType> tileTypes, Tile[,] Map)
    {
        foreach (Vector2I direction in dirs)
        {
            int nx = x + direction.X;
            int ny = y + direction.Y;

            if (nx >= 0 && nx < Map.GetLength(0) && ny >= 0 && ny < Map.GetLength(1))
            {
                if (tileTypes.Contains(Map[nx, ny].Type))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static NoiseData GetNoiseData(FastNoiseLite noise, Vector2I MapSize)
    {
        NoiseData noiseData = new NoiseData();
        noiseData.noiseValues = new float[MapSize.X, MapSize.Y];
        noiseData.min = 2;
        noiseData.max = -1;

        for (int x = 0; x < MapSize.X; x++)
        {
            for (int y = 0; y < MapSize.Y; y++)
            {

                float value = noise.GetNoise2D(x, y);
                noiseData.noiseValues[x, y] = value;

                if (value < noiseData.min) noiseData.min = value;
                if (value > noiseData.max) noiseData.max = value;
            }
        }
        return noiseData;
    }

    public static Color getTileTypeColor(TileType tileType)
    {
        return tileType switch
        {
            TileType.IceWater => new Color("0200ff"),
            TileType.Swamp => new Color("244d22"),
            TileType.TropicWater => new Color("1eeaff"),
            TileType.Snow => new Color("d8fbff"),
            TileType.StoneMountain => new Color("63524d"),
            TileType.Tundra => new Color("a7a8a8"),
            TileType.Taiga => new Color("082c0d"),
            TileType.RegularForest => new Color("49aa56"),
            TileType.TropicalForest => new Color("59f619"),
            TileType.Savanna => new Color("a89c5c"),
            TileType.Desert => new Color("f7e33e"),
            _ => throw new NotImplementedException()
        };
    }

    public static int getTileTypeAtlas(TileType tileType)
    {
        return tileType switch
        {
            TileType.IceWater => 10,
            TileType.Swamp => 9,
            TileType.TropicWater => 8,
            TileType.Snow => 7,
            TileType.StoneMountain => 6,
            TileType.Tundra => 5,
            TileType.Taiga => 4,
            TileType.RegularForest => 3,
            TileType.TropicalForest => 2,
            TileType.Savanna => 1,
            TileType.Desert => 0,
            _ => throw new NotImplementedException()
        };
    }

    public static PackedScene getResorsePrefabByType(ResorseType type)
    {
        return type switch
        {
            ResorseType.Wood => GD.Load<PackedScene>("res://Resourses/Prefabs/wood.tscn"),
            ResorseType.Gold => GD.Load<PackedScene>("res://Resourses/Prefabs/gold.tscn"),
            ResorseType.Iron => GD.Load<PackedScene>("res://Resourses/Prefabs/iron.tscn"),
            ResorseType.Stone => GD.Load<PackedScene>("res://Resourses/Prefabs/rock.tscn"),
            _ => throw new NotImplementedException()
        };
    }

    public static Node2D SetUpWorldNode(string Title, GeneratorData genData)
    {
        WorldScene node = new WorldScene();
        node.Name = Title;
        node.WorldPreview = ImageTexture.CreateFromImage(genData.BiomeMap);
        node.WorldName = "RandomGenWorldNameSoon";
        node.WorldSeed = genData.seed;
        node.GeneratorData = genData;
        return node;
    }

    public static Node2D SetNode2d(string Title)
    {
        Node2D node = new Node2D();
        node.Name = Title;
        return node;
    }


    public static Node2D SetNode2d(string Title, Node2D owner)
    {
        Node2D node = new Node2D();
        node.Name = Title;
        owner.AddChild(node);
        node.Owner = owner;

        return node;
    }

    public static Node2D SetNode2d(string Title, Node2D duplicate, Node2D owner)
    {
        Node2D node = (Node2D)duplicate.Duplicate();
        node.Name = Title;
        owner.AddChild(node);
        node.Owner = owner;

        return node;
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
                Console.Write(Math.Round(Convert.ToDecimal(array[j, i]), 2).ToString().PadLeft(3) + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("END ARRAY ===================== \n");
    }
}
