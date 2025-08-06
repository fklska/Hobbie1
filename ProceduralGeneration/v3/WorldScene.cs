using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    public TileMapLayer EnviromentLayer;
    public Node2D Enviroment;
    public Node2D Navigator;

    public Vector2I lastPlayerCell;

    public HashSet<Vector2I> currentActiveChunkMap = new HashSet<Vector2I>();

    public override void _Ready()
    {
        base._Ready();
        GenerationSettings.MapSize = GeneratorData.mapSize;
        GenerationSettings.RecalculateSetting();
        MainTileMapPrefab = GetNode<TileMapLayer>("DualMap");
        EnviromentLayer = GetNode<TileMapLayer>("EnviromentLayer");
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

    public async void InitionalChunkLoad()
    {
        Godot.Collections.Array<Vector2I> loadedChunks = GenerationUtils.ChunckAreaCoords(GenerationUtils.PixelToChunkCoord(GeneratorData.SpawnPoint), 4);
        foreach (Vector2I chunk in loadedChunks)
        {   
            await LoadChunk(chunk, MainTileMapPrefab, EnviromentLayer, Enviroment, Enviroment);
            Navigator.Call("bake_navigation_on_cell", chunk);
        }
        lastPlayerCell = GenerationUtils.PixelToChunkCoord(GeneratorData.SpawnPoint);
    }

    public async Task LoadChunk(Vector2I chunkCoord, TileMapLayer map, TileMapLayer EnvLayer, Node2D ResourseRootNode, Node2D owner)
    {
        if (chunkCoord.X > GenerationSettings.MAP_CHUNK_SIZE_X || chunkCoord.Y > GenerationSettings.MAP_CHUNK_SIZE_Y)
        {
            GD.PrintErr("Out of Map");
            return;
        }

        ChunkData chunk = GeneratorData.ChunkMap[chunkCoord.X][chunkCoord.Y];

        if (chunk.Active == true) { return; }

        chunk.Active = true;
        currentActiveChunkMap.Add(chunkCoord);
        int local_x = 0;
        for (int x = chunk.rect.X; x < chunk.rect.Z; x++)
        {
            int local_y = 0;
            for (int y = chunk.rect.Y; y < chunk.rect.W; y++)
            {
                map.SetCell(new Vector2I(x, y), GenerationUtils.getTileTypeAtlas(chunk.Map[local_x][local_y].Type), new Vector2I(2, 1), 0);
                
                ResorseType currType = chunk.Map[local_x][local_y].Resourse;
                if (currType != ResorseType.None)
                { 
                    EnvLayer.SetCell(new Vector2I(x, y), GenerationUtils.getResorseAtlacByType(currType), new Vector2I(0, 0), 0); 
                }
                // !!!OLD
                //GD.Print($"Coord: {x} {y}; Type: {currType}");
                /*if (currType != ResorseType.None)
                {
                    Node2D prefab = (Node2D)GenerationUtils.getResorsePrefabByType(chunk.Map[local_x][local_y].Resourse).Instantiate();
                    //Vector2 offset = new Vector2I(GD.RandRange(-50, 50), GD.RandRange(-50, 50));
                    chunk.Resourses.Add(prefab);
                    prefab.Position = new Vector2(x * GenerationUtils.TILE_SIZE, y * GenerationUtils.TILE_SIZE); //+ offset;

                    ResourseRootNode.CallDeferred("add_child", prefab);
                    //prefab.Owner = owner;
                }*/

                local_y++;
            }
            local_x++;

            await ToSignal(GetTree(), "process_frame");
        }
        Navigator.Call("bake_navigation_on_cell", chunkCoord);
    }

    public async Task UnloadChunk(Vector2I chunkCoord, TileMapLayer map, TileMapLayer EnvLayer)
    {
        if (chunkCoord.X > GenerationSettings.MAP_CHUNK_SIZE_X || chunkCoord.Y > GenerationSettings.MAP_CHUNK_SIZE_Y)
        {
            GD.PrintErr("Out of Map");
            return;
        }

        ChunkData chunk = GeneratorData.ChunkMap[chunkCoord.X][chunkCoord.Y];
        chunk.Active = false;
        currentActiveChunkMap.Remove(chunkCoord);
        int local_x = 0;
        for (int x = chunk.rect.X; x < chunk.rect.Z; x++)
        {
            int local_y = 0;
            for (int y = chunk.rect.Y; y < chunk.rect.W; y++)
            {
                map.EraseCell(new Vector2I(x, y));
                EnvLayer.EraseCell(new Vector2I(x, y));
                local_y++;
            }
            local_x++;
            await ToSignal(GetTree(), "process_frame");
        }

        /*foreach (Node2D res in chunk.Resourses)
        {
            res.CallDeferred("free");
        }
        await ToSignal(GetTree(), "process_frame");
        chunk.Resourses.Clear();*/
    }

    public async void UpdateChunkAroundPlayer(Vector2 coords)
    {
        Vector2I currentCell = GenerationUtils.PixelToChunkCoord(coords);

        if (currentCell !=  lastPlayerCell)
        {
            Vector2I diff = currentCell - lastPlayerCell;

            if (Math.Abs(diff.X) > 1 || Math.Abs(diff.Y) > 1)
            {
                GD.PrintErr("WTF! TELEPORT?");
                lastPlayerCell = currentCell;
                return;
            }

            Godot.Collections.Array<Vector2I> chunksToDelete = GenerationUtils.GetSideChunkFromDirection(-diff, lastPlayerCell, 4);
            Godot.Collections.Array<Vector2I> chunksToLoad = GenerationUtils.GetSideChunkFromDirection(diff, currentCell, 4);

            foreach(Vector2I chunk in chunksToDelete)
            {
                await UnloadChunk(chunk, MainTileMapPrefab, EnviromentLayer);
            }

            foreach (Vector2I chunk in chunksToLoad)
            {
                await LoadChunk(chunk, MainTileMapPrefab, EnviromentLayer, Enviroment, Enviroment);
            }
            //GD.Print($"Diff {diff}; Curr: {currentCell}; Delete: {chunksToDelete}; Load: {chunksToLoad}");
            lastPlayerCell = currentCell;
        }
    }

    public Tile getTile(Vector2I chunk, Vector2I localCoords)
    {
        if (chunk < Vector2I.Zero || chunk > new Vector2(GenerationSettings.MAP_CHUNK_SIZE_X, GenerationSettings.MAP_CHUNK_SIZE_Y))
        {
            GD.Print("Chunk Out Of Map");
            return new Tile();
        }

        if (localCoords < Vector2I.Zero || localCoords > new Vector2I(GenerationSettings.CHUNK_SIZE, GenerationSettings.CHUNK_SIZE))
        {
            GD.Print("Incorrect local coords");
            return new Tile();
        }

        return GeneratorData.ChunkMap[chunk.X][chunk.Y].Map[localCoords.X][localCoords.Y];
    }

    public Vector2I lastcell = Vector2I.Zero;
    public void GetCell()
    {
        Vector2 mouseCoor = GetGlobalMousePosition();
        Vector2I cell = MainTileMapPrefab.LocalToMap(mouseCoor);
        if (lastcell != cell)
        {
            lastcell = cell;
            if (cell.X < GeneratorData.mapSize.X && cell.Y < GeneratorData.mapSize.Y && DebugInfo)
            {
                Vector2I chunkCell = cell / GenerationSettings.CHUNK_SIZE;
                GD.Print(chunkCell);
                /*float height = GeneratorData.Map[cell.X][cell.Y].heightValue;
                float heat = GeneratorData.Map[cell.X][cell.Y].heatValue;
                float moisture = GeneratorData.Map[cell.X][cell.Y].moistureValue;
                float treeValue = GeneratorData.Map[cell.X][cell.Y].treeResValue;
                float oreValue = GeneratorData.Map[cell.X][cell.Y].oreResValue;
                TileType tileType = GeneratorData.Map[cell.X][cell.Y].Type;
                ResorseType resType = GeneratorData.Map[cell.X][cell.Y].Resourse;
                //GD.Print(String.Format("Coords: {0};\nHeight: {1};\nHeat: {2};\nMoisture: {3};\nBiome: {4};\n", [cell, height, heat, moisture, tileType]));
                GD.Print($"Coord: {cell}, Biome: {tileType}, Resourse: {resType}");
                GD.Print($"Height: {height}, Heat: {heat}, Moist: {moisture}");
                GD.Print($"TreeValue: {treeValue}, oreValue: {oreValue} \n");*/
            }
        }
    }
}
