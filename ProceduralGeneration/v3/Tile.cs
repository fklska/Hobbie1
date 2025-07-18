using Godot;
using System;

[GlobalClass]
public partial class Tile: Resource
{
    [Export] public float heightValue = 0;
    [Export] public float heatValue = 0;
    [Export] public float moistureValue = 0;
    [Export] public TileType Type = TileType.None;
    [Export] public ResorseType Resourse = ResorseType.None;

    public void UpdateInfo(float heightValue, float heatValue, float moistureValue, TileType Type)
    {
        this.heightValue = heightValue;
        this.heatValue = heatValue;
        this.moistureValue = moistureValue;
        this.Type = Type;
    }
}
