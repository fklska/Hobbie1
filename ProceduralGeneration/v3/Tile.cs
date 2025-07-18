using Godot;
using System;

[Tool]
[GlobalClass]
public partial class Tile: Resource
{
    [Export] public float heightValue = 0;
    [Export] public float heatValue = 0;
    [Export] public float moistureValue = 0;
    [Export] public float treeResValue = 0;
    [Export] public float oreResValue = 0;
    [Export] public TileType Type = TileType.None;
    [Export] public ResorseType Resourse = ResorseType.None;

    public void UpdateInfo(float heightValue, float heatValue, float moistureValue, TileType Type)
    {
        this.heightValue = heightValue;
        this.heatValue = heatValue;
        this.moistureValue = moistureValue;
        this.Type = Type;
    }

    public void SetResorsesValues(float treeResValue, float oreResValue, ResorseType ResourseType)
    {
        this.treeResValue = treeResValue;
        this.oreResValue = oreResValue;
        this.Resourse = ResourseType;
    }
}
