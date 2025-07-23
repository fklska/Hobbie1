using Godot;
using System;

public static partial class GenerationSettings
{
    public static bool NewSeed = true;
    public static Vector2I MapSize = new Vector2I(128, 128);

    public const int TILE_SIZE = 64;
    public const int CHUNK_SIZE = 16;
}
