using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class GeneratorV3 : Node2D
{
    [Export] public GeneratorData genData;
    [Export] public Godot.Collections.Array<GenerationStep> steps;
    public Image map_render = new Image();


    public override void _Ready()
    {
        base._Ready();
        foreach (var step in steps)
        {
            step.Execute(genData);
        }
    }
}
