using Godot;
using System;

[GlobalClass]
public abstract partial class GenerationStep : Resource
{
    [Export] public FastNoiseLite noise;

    public abstract void Execute(GeneratorData gen_data);
}
