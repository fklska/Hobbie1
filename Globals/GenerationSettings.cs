using Godot;
using System;

public static partial class GenerationSettings
{
    public static bool NewSeed = true;
    public static Vector2I MapSize = new Vector2I(128, 128);


    public const int TILE_SIZE = 64;
    public const int CHUNK_SIZE = 8;

    public static int MAP_CHUNK_SIZE_X = MapSize.X / CHUNK_SIZE;
    public static int MAP_CHUNK_SIZE_Y = MapSize.Y / CHUNK_SIZE;

    public static void RecalculateSetting()
    {
        MAP_CHUNK_SIZE_X = MapSize.X / CHUNK_SIZE;
        MAP_CHUNK_SIZE_Y = MapSize.Y / CHUNK_SIZE;
    }
}
