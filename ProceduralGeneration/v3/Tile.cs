using Godot;
using System;

public class Tile
{
    public float heightValue;
    public float heatValue;
    public float moistureValue;
    public TileType Type;
    public ResorseType Resourse = ResorseType.None;

    public Tile(float heightValue, float heatValue, float moistureValue, TileType type)
    {
        this.heightValue = heightValue;
        this.heatValue = heatValue;
        this.moistureValue = moistureValue;
        Type = type;

    }

}
