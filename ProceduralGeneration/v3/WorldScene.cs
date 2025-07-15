using Godot;
using System;

[Tool]
[GlobalClass]
public partial class WorldScene : Node2D
{
    [Export] public GeneratorData GeneratorData { get; set; }
    [Export] public string WorldName;
    [Export] public int WorldSeed;
    [Export] public Texture2D WorldPreview;
}
