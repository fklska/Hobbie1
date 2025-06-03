using Godot;
using System;

[GlobalClass]
public abstract partial class GenerationStep : Resource
{
    [Export] public FastNoiseLite noise;
    [Export] public bool Enabled = true;

    public abstract void Execute(GeneratorData gen_data);
}
