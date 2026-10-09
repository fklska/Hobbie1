using Godot;

[GlobalClass]
public abstract partial class GenerationStep : Resource
{
    [Export] public bool Enabled = true;

    public abstract void Execute(WorldGen gen);
}
