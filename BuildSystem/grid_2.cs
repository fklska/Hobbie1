using Godot;
using System;


[Tool]
public partial class grid_2 : Node2D
{
    [Export] Vector2I cellSize;
    [Export] Vector2I gridzSize;
    [Export] Color cellColor;
    Vector2I current_cell = Vector2I.Zero;

    public override void _Process(double delta)
    {
        if (current_cell != pixelToCell(GetGlobalMousePosition()))
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        DrawGrid();
    }

    public void DrawGrid()
    {
        for (int x = -gridzSize.X/2; x <= gridzSize.X / 2; x++)
        {
            for (int y = -gridzSize.Y/2; y <= gridzSize.Y/2; y++)
            {
                current_cell = pixelToCell(GetGlobalMousePosition());
                Rect2I rect = new Rect2I((new Vector2I(x, y) + current_cell) * cellSize, cellSize);
                Color mask = cellColor;
                mask.A = mask.A / (1 + 10*(Math.Abs(x) + Math.Abs(y)));
                DrawRect(rect, mask, false);
            }
        }
    }

    public Vector2I pixelToCell(Vector2 coords)
    {
        int x = Convert.ToInt32(coords.X) / cellSize.X;
        if (Mathf.Sign(coords.X) == -1)
        {
            x -= 1;
        }
        int y = Convert.ToInt32(coords.Y) / cellSize.Y;
        if (Mathf.Sign(coords.Y) == 1)
        {
            y -= 1;
        }
        return new Vector2I(x, y);
    }
}
