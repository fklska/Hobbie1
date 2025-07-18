using Godot;
using System;

public partial class TestSaveRes : Node2D
{
    [Export] public TetsSave tetsSave;

    public override void _Ready()
    {
        GeneratorData saved_res = ResourceLoader.Load<GeneratorData>("res://SavedWorlds/Forgotten Land of the Ancients.tres");
        GD.Print(saved_res);
        for (int x = 0; x < saved_res.mapSize.X; x++)
        {
            for (int y = 0; y < saved_res.mapSize.Y; y++)
            {
                GD.Print(saved_res.Map[x][y].GetType());
            }
        }
    }

}
