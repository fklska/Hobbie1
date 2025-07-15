using Godot;
using System;

[GlobalClass]
public partial class Tile: Resource
{
    [Export] public float heightValue;
    [Export] public float heatValue;
    [Export] public float moistureValue;
    [Export] public TileType Type = TileType.None;
    [Export] public ResorseType Resourse = ResorseType.None;

    public void MyConstructor(float heightValue, float heatValue, float moistureValue, TileType Type)
    {
        this.heightValue = heightValue;
        this.heatValue = heatValue;
        this.moistureValue = moistureValue;
        this.Type = Type;
    }
}
