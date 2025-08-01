using Godot;
using Godot1.Globals;
using System;
using System.Threading.Tasks;

public partial class PlayerMainCharacter : CharacterBody2D
{
    [Export] public Control inventory;
    [Export] public Control HotBar;

    [ExportCategory("Stats")]
    [Export] public int AGILITY = 10;
    [Export] public int STRENCH = 10;
    [Export] public int INTELECT = 10;
    [Export] public float SPEED = 20;

    [ExportCategory("Render")]
    [Export] public AnimationPlayer anim;

    public WorldScene WorldScene;
    public Vector2I lastChunkCell;
    public enum State { RUN, ATTACK, ACTION}

    public override void _Ready()
    {
        WorldScene = GetTree().Root.GetNode<WorldScene>("Map");
        lastChunkCell = Utils.GetChunkCoords(GlobalPosition);
    }

    public override void _Process(double delta)
    {
        UpdateChunks();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }

    public void Run()
    {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
        if (direction != Vector2.Zero)
        {
            Velocity = direction * SPEED * AGILITY;
            anim.Play();
        }
    }

    public void UpdateChunks()
    {
        if (IsInstanceValid(WorldScene)) WorldScene.UpdateChunkAroundPlayer(GlobalPosition);
    }
}
