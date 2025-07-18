using Godot;
using System;

[Tool]
[GlobalClass]
public partial class WorldScene : Node2D
{
    [Export] public GeneratorData GeneratorData;
    [Export] public string GenDataPath;
    [Export] public string WorldName;
    [Export] public int WorldSeed;
    [Export] public Texture2D WorldPreview;

    [Export] public bool DebugInfo;

    public TileMapLayer MainTileMapPrefab;

    public override void _Ready()
    {
        base._Ready();
        MainTileMapPrefab = GetNode<TileMapLayer>("DualMap");
        GeneratorData = ResourceLoader.Load<GeneratorData>(GenDataPath);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        GetCell();
    }
    public Vector2I lastcell = Vector2I.Zero;
    public void GetCell()
    {
        Vector2 mouseCoor = GetGlobalMousePosition();
        Vector2I cell = MainTileMapPrefab.LocalToMap(mouseCoor);
        if (cell.X < GeneratorData.mapSize.X && cell.Y < GeneratorData.mapSize.Y && DebugInfo && cell != lastcell)
        {
            lastcell = cell;
            GD.Print(GeneratorData.Map[cell.X][cell.Y].Type);
            float height = GeneratorData.Map[cell.X][cell.Y].heightValue;
            float heat = GeneratorData.Map[cell.X][cell.Y].heatValue;
            float moisture = GeneratorData.Map[cell.X][cell.Y].moistureValue;
            float treeValue = GeneratorData.Map[cell.X][cell.Y].treeResValue;
            float oreValue = GeneratorData.Map[cell.X][cell.Y].oreResValue;
            TileType tileType = GeneratorData.Map[cell.X][cell.Y].Type;
            ResorseType resType = GeneratorData.Map[cell.X][cell.Y].Resourse;
            //GD.Print(String.Format("Coords: {0};\nHeight: {1};\nHeat: {2};\nMoisture: {3};\nBiome: {4};\n", [cell, height, heat, moisture, tileType]));
            GD.Print($"Coord: {cell}, Biome: {tileType}, Resourse: {resType}");
            GD.Print($"Height: {height}, Heat: {heat}, Moist: {moisture}");
            GD.Print($"TreeValue: {treeValue}, oreValue: {oreValue} \n");
        }
    }
}
