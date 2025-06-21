using Godot;
using System;

[GlobalClass]
public abstract partial class GenerationStep : Resource
{
    [Export] public bool Enabled = true;

    public abstract void Execute(GeneratorData gen_data);
}
