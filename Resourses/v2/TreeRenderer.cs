using Godot;
using System;

[Tool]
[GlobalClass]
public partial class TreeRenderer : Node2D
{
    GeneratorData genData = ResourceLoader.Load<GeneratorData>("res://SavedWorlds/Mysterious Land of Doom.tres");
    [Export] public MultiMeshInstance2D MultiMeshInstance2D;
    [Export] public Transform2D Transform2D;

    public override void _Ready()
    {
    }
}
