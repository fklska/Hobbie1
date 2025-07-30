using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static Godot.TextServer;

public static partial class GenerationUtils
{
    public static RandomNumberGenerator rnd = new RandomNumberGenerator();
    public static readonly List<Vector2I> dirs = new List<Vector2I>
    {
        new Vector2I(-1, -1), // left top
        new Vector2I(0, -1), // up in gd
        new Vector2I(1, -1), // right top
        new Vector2I(-1, 0), // left
        //new Vector2I(0, 0), // self
        new Vector2I(1, 0), // right
        new Vector2I(-1, 1), // lef down
        new Vector2I(0, 1), // down in GD
        new Vector2I(1, 1), // right down
    };
    private static string[] Adjectives = { "Dark", "Mysterious", "Ancient", "Frozen", "Lost", "Forgotten", "Eternal" };
    private static string[] Nouns = { "Realm", "Empire", "Kingdom", "Land", "Dominion", "World", "Dimension" };
    private static string[] Suffixes = { "of Legends", "of Shadows", "of Doom", "of Eternity", "of the Ancients" };

    public const int TILE_SIZE = 64;

    public static HashSet<Vector2I> GetEdgeTiles(List<TileType> typesToSearchFor, List<TileType> typesAdjestedTo, GeneratorData genData)
    {
        HashSet<Vector2I> borderLine = new HashSet<Vector2I>();

        for(int x  = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0;y < genData.mapSize.Y; y++)
            {
                if (typesToSearchFor.Contains(genData.Map[x][y].Type) && IsAdjacentToTiles(x, y, typesAdjestedTo, genData))
                {
                    borderLine.Add(new Vector2I(x, y));
                }
            }
        }

        return borderLine;
    }

    public static HashSet<Vector2I> GetEdgeTiles(List<TileType> typesToSearchFor, List<TileType> typesAdjestedTo, ChunkData chunk)
    {
        HashSet<Vector2I> borderLine = new HashSet<Vector2I>();

        for (int x = 0; x < GenerationSettings.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < GenerationSettings.CHUNK_SIZE; y++)
            {
                if (typesToSearchFor.Contains(chunk.Map[x][y].Type) && IsAdjacentToTiles(x, y, typesAdjestedTo, chunk))
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
                        if (validTiles.Contains(genData.Map[nCoords.X][nCoords.Y].Type))
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

    public static HashSet<Vector2I> ExpandEdgeTiles(HashSet<Vector2I> initialLayer, int widht, List<TileType> validTiles, ChunkData chunk)
    {
        HashSet<Vector2I> LastExpendedTiles = new HashSet<Vector2I>();
        HashSet<Vector2I> currentOutTileLine = new HashSet<Vector2I>(initialLayer); // Нужно создать новый хешсет иначе ошибка

        for (int i = 0; i < widht; i++)
        {
            foreach (Vector2I tileCoor in currentOutTileLine)
            {
                foreach (Vector2I dir in dirs)
                {
                    Vector2I nCoords = tileCoor + dir;
                    if (nCoords.X >= 0 && nCoords.X < GenerationSettings.CHUNK_SIZE && nCoords.Y >= 0 && nCoords.Y < GenerationSettings.CHUNK_SIZE)
                    {
                        if (validTiles.Contains(chunk.Map[nCoords.X][nCoords.Y].Type))
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

    public static bool IsAdjacentToTiles(int x, int y, List<TileType> tileTypes, GeneratorData genData)
    {
        foreach (Vector2I direction in dirs)
        {
            int nx = x + direction.X;
            int ny = y + direction.Y;

            if (nx >= 0 && nx < genData.mapSize.X && ny >= 0 && ny < genData.mapSize.Y)
            {
                if (tileTypes.Contains(genData.Map[nx][ny].Type))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsAdjacentToTiles(int x, int y, List<TileType> tileTypes, ChunkData chunk)
    {
        foreach (Vector2I direction in dirs)
        {
            int nx = x + direction.X;
            int ny = y + direction.Y;

            if (nx >= 0 && nx < GenerationSettings.CHUNK_SIZE && ny >= 0 && ny < GenerationSettings.CHUNK_SIZE)
            {
                if (tileTypes.Contains(chunk.Map[nx][ny].Type))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsEdgeTile(int x, int y, TileType tileType, GeneratorData genData)
    {
        foreach (Vector2I direction in dirs)
        {
            int nx = x + direction.X;
            int ny = y + direction.Y;

            if (nx >= 0 && nx < genData.mapSize.X && ny >= 0 && ny < genData.mapSize.Y)
            {
                if (genData.Map[nx][ny].Type != tileType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsEdgeTile(int x, int y, TileType tileType, ChunkData chunk)
    {
        foreach (Vector2I direction in dirs)
        {
            int nx = x + direction.X;
            int ny = y + direction.Y;

            if (nx >= 0 && nx < GenerationSettings.CHUNK_SIZE && ny >= 0 && ny < GenerationSettings.CHUNK_SIZE)
            {
                if (chunk.Map[nx][ny].Type != tileType)
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

    public static Vector2I PixelToChunkCoord(Vector2 coords)
    {
        if(coords.X < 0 || coords.Y < 0)
        {
            GD.PrintErr("Non valid coord < 0");
            return Vector2I.Zero;
        }
        int pixel_chunk_size = GenerationSettings.CHUNK_SIZE * GenerationSettings.TILE_SIZE;
        return new Vector2I((int)coords.X / pixel_chunk_size, (int)coords.Y / pixel_chunk_size);
    }

    public static Godot.Collections.Array<Vector2I> ChunckAreaCoords(Vector2I ChunkCoord, int distance=2)
    {
        Godot.Collections.Array<Vector2I> chunks = new Godot.Collections.Array<Vector2I>();


        for (int dx = -distance; dx <= distance; dx++)
        {
            for (int dy = -distance; dy <= distance; dy++)
            {
                int nx = ChunkCoord.X + dx;
                int ny = ChunkCoord.Y + dy;

                if (nx >= 0 && nx < GenerationSettings.MAP_CHUNK_SIZE_X &&
                    ny >= 0 && ny < GenerationSettings.MAP_CHUNK_SIZE_Y)
                {
                    chunks.Add(new Vector2I(nx, ny));
                }
            }
        }
        chunks.Add(ChunkCoord);

        return chunks;
    }

    public static Godot.Collections.Array<Vector2I> GetSideChunkFromDirection(Vector2I directionOffset, Vector2I currCenterChunk, int distance = 2)
    {
        var chunks = new Godot.Collections.Array<Vector2I>();

        if (directionOffset.X != 0)
        {
            int x = currCenterChunk.X + directionOffset.X * distance;

            for (int dy = -distance; dy <= distance; dy++)
            {
                int y = currCenterChunk.Y + dy;

                if (x >= 0 && x < GenerationSettings.MAP_CHUNK_SIZE_X && y >= 0 && y < GenerationSettings.MAP_CHUNK_SIZE_Y)
                {
                    chunks.Add(new Vector2I(x, y));
                }
            }
        }

        if (directionOffset.Y != 0)
        {
            int y = currCenterChunk.Y + directionOffset.Y * distance;

            for (int dx = -distance; dx <= distance; dx++)
            {
                int x = currCenterChunk.X + dx;

                if (x >= 0 && x < GenerationSettings.MAP_CHUNK_SIZE_X && y >= 0 && y < GenerationSettings.MAP_CHUNK_SIZE_Y)
                {
                    chunks.Add(new Vector2I(x, y));
                }
            }
        }

        return chunks;
    }

    public static Color getTileTypeColor(TileType tileType)
    {
        return tileType switch
        {
            TileType.DeepWater => new Color("000057"),
            TileType.TropicWater => new Color("a3f5fd"),
            TileType.Snow => new Color("d8fbff"),
            TileType.Tundra => new Color("a7a8a8"),
            TileType.Taiga => new Color("153d13"),
            TileType.RegularForest => new Color("8eba43"),
            TileType.TropicalForest => new Color("4b8a00"),
            TileType.Savanna => new Color("a89c5c"),
            TileType.Desert => new Color("f7e33e"),
            _ => throw new NotImplementedException()
        };
    }

    public static int getTileTypeAtlas(TileType tileType)
    {
        return tileType switch
        {
            TileType.DeepWater => 10,
            //TileType.Swamp => 9,
            TileType.TropicWater => 8,
            TileType.Snow => 7,
            //TileType.StoneMountain => 6,
            TileType.Tundra => 5,
            TileType.Taiga => 4,
            TileType.RegularForest => 3,
            TileType.TropicalForest => 2,
            TileType.Savanna => 1,
            TileType.Desert => 0,
            _ => throw new NotImplementedException()
        };
    }
    // Current
    public static int getResorseAtlacByType(ResorseType type)
    {
        return type switch
        {
            ResorseType.GiantWood => 0,
            ResorseType.GiantSnowWood => 1,
            ResorseType.MediumWood => 2,
            ResorseType.MediumSnowWood => 3,
            ResorseType.SmallWood => 4,
            ResorseType.SmallSnowWood => 5,
            _ => 0
        };
    }
    // OLD
    public static PackedScene getResorsePrefabByType(ResorseType type)
    {
        return type switch
        {
            ResorseType.GiantWood => GD.Load<PackedScene>("res://Resourses/Prefabs/GiantWood.tscn"),
            ResorseType.GiantSnowWood => GD.Load<PackedScene>("res://Resourses/Prefabs/GiantSnowWood.tscn"),
            ResorseType.MediumWood => GD.Load<PackedScene>("res://Resourses/Prefabs/MediumWood.tscn"),
            ResorseType.MediumSnowWood => GD.Load<PackedScene>("res://Resourses/Prefabs/MediumSnowWood.tscn"),
            ResorseType.SmallWood => GD.Load<PackedScene>("res://Resourses/Prefabs/SmallWood.tscn"),
            ResorseType.SmallSnowWood => GD.Load<PackedScene>("res://Resourses/Prefabs/SmallSnowWood.tscn"),
            ResorseType.Gold => GD.Load<PackedScene>("res://Resourses/Prefabs/gold.tscn"),
            ResorseType.Iron => GD.Load<PackedScene>("res://Resourses/Prefabs/iron.tscn"),
            ResorseType.Stone => GD.Load<PackedScene>("res://Resourses/Prefabs/rock.tscn"),
            _ => GD.Load<PackedScene>("res://Resourses/Prefabs/Dummy.tscn")
        };
    }

    public static Node2D SetUpWorldNode(string Title, GeneratorData genData)
    {
        WorldScene node = new WorldScene();
        node.Name = Title;
        node.GeneratorData = genData;
        node.WorldPreview = ImageTexture.CreateFromImage(genData.BiomeMap);
        node.WorldName = genData.WorldName;
        node.WorldSeed = genData.seed;
        node.GenDataPath = genData.ResourcePath;
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
        node.YSortEnabled = true;

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

    private static Random _random = new Random();
    public static string GenerateNameWorld()
    {
        // Выбираем случайные части
        string adjective = Adjectives[_random.Next(Adjectives.Length)];
        string noun = Nouns[_random.Next(Nouns.Length)];
        string suffix = Suffixes[_random.Next(Suffixes.Length)];

        // Собираем имя (например: "Dark Realm of Shadows")
        return $"{adjective} {noun} {suffix}";
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
