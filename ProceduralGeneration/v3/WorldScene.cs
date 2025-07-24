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
    public Node2D Enviroment;
    public Node2D Navigator;

    public override void _Ready()
    {
        base._Ready();
        GenerationSettings.MapSize = GeneratorData.mapSize;
        GenerationSettings.RecalculateSetting();
        MainTileMapPrefab = GetNode<TileMapLayer>("DualMap");
        Enviroment = GetNode<Node2D>("Enviroment");
        GeneratorData = ResourceLoader.Load<GeneratorData>(GenDataPath);
        Navigator = GetTree().Root.GetNode<Node2D>("GlobalNavigation");
        InitionalChunkLoad();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        GetCell();
    }

    public void InitionalChunkLoad()
    {
        Godot.Collections.Array<Vector2I> loadedChunks = GenerationUtils.ChunckAreaCoords(GenerationUtils.PixelToChunkCoord(GeneratorData.SpawnPoint));
        foreach (Vector2I chunk in loadedChunks)
        {   
            LoadChunk(chunk, MainTileMapPrefab, Enviroment, Enviroment);
            Navigator.CallDeferred("bake_navigation_on_cell", chunk);
        }
    }

    public void LoadChunk(Vector2I chunkCoord, TileMapLayer map, Node2D ResourseRootNode, Node2D owner)
    {
        if (chunkCoord.X > GenerationSettings.MAP_CHUNK_SIZE_X || chunkCoord.Y > GenerationSettings.MAP_CHUNK_SIZE_Y)
        {
            GD.PrintErr("Out of Map");
            return;
        }

        ChunkData chunk = GeneratorData.ChunkMap[chunkCoord.X][chunkCoord.Y];

        int local_x = 0;
        for (int x = chunk.rect.X; x < chunk.rect.Z; x++)
        {
            int local_y = 0;
            for (int y = chunk.rect.Y; y < chunk.rect.W; y++)
            {
                map.SetCell(new Vector2I(x, y), GenerationUtils.getTileTypeAtlas(chunk.Map[local_x][local_y].Type), new Vector2I(2, 1), 0);
                ResorseType currType = chunk.Map[local_x][local_y].Resourse;
                //GD.Print($"Coord: {x} {y}; Type: {currType}");
                if (currType != ResorseType.None)
                {
                    Node2D prefab = (Node2D)GenerationUtils.getResorsePrefabByType(chunk.Map[local_x][local_y].Resourse).Instantiate();
                    //Vector2 offset = new Vector2I(GD.RandRange(-50, 50), GD.RandRange(-50, 50));
                    chunk.Resourses.Add(prefab);
                    prefab.Position = new Vector2(x * GenerationUtils.TILE_SIZE, y * GenerationUtils.TILE_SIZE); //+ offset;

                    ResourseRootNode.AddChild(prefab);
                    //prefab.Owner = owner;
                }

                local_y++;
            }
            local_x++;
        }
    }

    public void UnloadChunk(Vector2I chunkCoord, TileMapLayer map)
    {
        if (chunkCoord.X > GenerationSettings.MAP_CHUNK_SIZE_X || chunkCoord.Y > GenerationSettings.MAP_CHUNK_SIZE_Y)
        {
            GD.PrintErr("Out of Map");
            return;
        }

        ChunkData chunk = GeneratorData.ChunkMap[chunkCoord.X][chunkCoord.Y];

        int local_x = 0;
        for (int x = chunk.rect.X; x < chunk.rect.Z; x++)
        {
            int local_y = 0;
            for (int y = chunk.rect.Y; y < chunk.rect.W; y++)
            {
                map.EraseCell(new Vector2I(x, y));

                foreach (Node2D res in chunk.Resourses)
                {
                    res.QueueFree();
                }

                local_y++;
            }
            local_x++;
        }
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
