using Godot;
using System;

[GlobalClass]
public partial class GlobalSettings : Node
{
    public bool NewSeed = true;
    public Vector2I MapSize = new Vector2I();
}
