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
        if (IsInstanceValid(genData))
        {int size = genData.TreeCoords.Count;
        MultiMeshInstance2D.Multimesh.InstanceCount = size;
            for (int i = 0; i < size; i++)
            {
                MultiMeshInstance2D.Multimesh.SetInstanceTransform2D(i, Transform2D.Identity.Translated(genData.TreeCoords[i]));
            }
        }
    }
}
