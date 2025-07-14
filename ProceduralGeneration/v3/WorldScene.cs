using Godot;
using System;

[Tool]
[GlobalClass]
public partial class WorldScene : Node2D
{
    public GeneratorData GeneratorData { get; set; }
    public string WorldName;
    public int WorldSeed;
    public Texture2D WorldPreview;
}
