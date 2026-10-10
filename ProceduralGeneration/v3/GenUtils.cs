using Godot;
using System;

public static partial class GenerationUtils
{
    public static RandomNumberGenerator rnd = new RandomNumberGenerator();
    private static string[] Adjectives = { "Dark", "Mysterious", "Ancient", "Frozen", "Lost", "Forgotten", "Eternal" };
    private static string[] Nouns = { "Realm", "Empire", "Kingdom", "Land", "Dominion", "World", "Dimension" };
    private static string[] Suffixes = { "of Legends", "of Shadows", "of Doom", "of Eternity", "of the Ancients" };

    public const int TILE_SIZE = 64;

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
            TileType.Swamp => new Color("4f5d2f"),
            TileType.StoneMountain => new Color("6e6a64"),
            _ => throw new NotImplementedException()
        };
    }

    public static int getTileTypeAtlas(TileType tileType)
    {
        return tileType switch
        {
            TileType.DeepWater => 10,
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

    private static Random _random = new Random();
    public static string GenerateNameWorld()
    {
        string adjective = Adjectives[_random.Next(Adjectives.Length)];
        string noun = Nouns[_random.Next(Nouns.Length)];
        string suffix = Suffixes[_random.Next(Suffixes.Length)];
        return $"{adjective} {noun} {suffix}";
    }
}
