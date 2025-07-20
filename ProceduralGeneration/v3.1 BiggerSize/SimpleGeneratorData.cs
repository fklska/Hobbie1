using Godot;
using System;


[Tool]
[GlobalClass]
public partial class SimpleGeneratorData : Resource
{
    [Export] public Vector2I mapSize;
    [Export] public Vector2I SpawnPoint;

    [Export] public String WorldName;

    [Export] public Image BiomeMap;
    [Export] public int seed;
    [Export] public string fullDataPath;
}
