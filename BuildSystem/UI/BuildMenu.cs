using Godot;
using System;

public partial class BuildMenu : TabContainer
{
    [Export] public TextureRect DisplayFlyobj;
    public Texture2D flyobj;


    [Export] public Texture2D TownHallTexture; 
    public override void _Process(double delta)
    {
        if(flyobj != null)
        {
            DisplayFlyobj.Position = GetIntPosition();
        }
    }


    public Vector2I GetIntPosition()
    {
        return Grid.pixelToCell(GetGlobalMousePosition());
    }
    public void OpenMenu()
    {
        Visible = true;
    }

    public void CloseMenu()
    {
        Visible = false;
    }

    public void _on_town_hall_pressed()
    {
        CloseMenu();
        flyobj = TownHallTexture;
        DisplayFlyobj.Texture = flyobj;
        Grid.buildMode = true;
    }


}
