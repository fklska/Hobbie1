using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Tool]
[GlobalClass]
public partial class WorldScene : Node2D
{
	[Signal] public delegate void TileHarvestedEventHandler(Vector2I cell, ResorseType type);

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
	private readonly HashSet<Vector2I> loadedChunks = new HashSet<Vector2I>();

	public const int ObjectsZ = 2;

	public double LoadBudgetMs;
	private ulong sliceFrame;
	private ulong sliceStart;

	public override void _Ready()
	{
		base._Ready();
		GenerationSettings.MapSize = GeneratorData.mapSize;
		GenerationSettings.RecalculateSetting();
		MainTileMapPrefab = GetNode<TileMapLayer>("DualMap");
		EnviromentLayer = GetNode<TileMapLayer>("EnviromentLayer");
		Enviroment = GetNode<Node2D>("Enviroment");
		YSortEnabled = true;
		EnviromentLayer.YSortEnabled = true;
		EnviromentLayer.ZIndex = ObjectsZ;
		Enviroment.YSortEnabled = true;
		Enviroment.ZIndex = 0;
		GeneratorData = ResourceLoader.Load<GeneratorData>(GenDataPath, null, ResourceLoader.CacheMode.Ignore);
		// Navigator = GetTree().Root.GetNode<Node2D>("GlobalNavigation");
		InitionalChunkLoad();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		GetCell();
	}

	public async void InitionalChunkLoad()
	{
		Vector2I center = GenerationUtils.PixelToChunkCoord(GeneratorData.SpawnPoint);
		List<Vector2I> chunks = new List<Vector2I>(GenerationUtils.ChunckAreaCoords(center, 4));
		chunks.Sort((a, b) => (a - center).LengthSquared().CompareTo((b - center).LengthSquared()));
		foreach (Vector2I chunk in chunks)
		{   
			await LoadChunk(chunk, MainTileMapPrefab, EnviromentLayer, Enviroment, Enviroment);
			// Navigator.Call("bake_navigation_on_cell", chunk);
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

			await NextColumn();
		}
		if (chunk.Active) loadedChunks.Add(chunkCoord);
		// Navigator.Call("bake_navigation_on_cell", chunkCoord);
	}

	private async Task NextColumn()
	{
		ulong frame = Engine.GetProcessFrames();
		if (frame != sliceFrame)
		{
			sliceFrame = frame;
			sliceStart = Time.GetTicksUsec();
		}
		if (LoadBudgetMs > 0 && Time.GetTicksUsec() - sliceStart < LoadBudgetMs * 1000) return;
		await ToSignal(GetTree(), "process_frame");
	}

	public float LoadedAround(Vector2 at, int distance)
	{
		Godot.Collections.Array<Vector2I> area = GenerationUtils.ChunckAreaCoords(GenerationUtils.PixelToChunkCoord(at), distance);
		int done = 0;
		foreach (Vector2I chunk in area)
		{
			if (loadedChunks.Contains(chunk)) done++;
		}
		return area.Count == 0 ? 1f : (float)done / area.Count;
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
		loadedChunks.Remove(chunkCoord);
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

	public Tile GetTileAt(Vector2I globalCell)
	{
		if (globalCell.X < 0 || globalCell.Y < 0 || globalCell.X >= GeneratorData.mapSize.X || globalCell.Y >= GeneratorData.mapSize.Y) return null;
		Vector2I chunk = globalCell / GenerationSettings.CHUNK_SIZE;
		return GeneratorData.ChunkMap[chunk.X][chunk.Y].Map[globalCell.X % GenerationSettings.CHUNK_SIZE][globalCell.Y % GenerationSettings.CHUNK_SIZE];
	}

	public ResorseType GetResourceAt(Vector2I globalCell) => GetTileAt(globalCell)?.Resourse ?? ResorseType.None;

	public ResorseType HarvestTile(Vector2I globalCell)
	{
		Tile tile = GetTileAt(globalCell);
		if (tile == null || tile.Resourse == ResorseType.None) return ResorseType.None;

		ResorseType type = tile.Resourse;
		tile.Resourse = ResorseType.None;
		EnviromentLayer.EraseCell(globalCell);
		EmitSignal(SignalName.TileHarvested, globalCell, (int)type);
		return type;
	}

	public void RestoreTile(Vector2I globalCell, ResorseType type)
	{
		Tile tile = GetTileAt(globalCell);
		if (tile == null) return;

		tile.Resourse = type;
		if (currentActiveChunkMap.Contains(globalCell / GenerationSettings.CHUNK_SIZE))
		{
			EnviromentLayer.SetCell(globalCell, GenerationUtils.getResorseAtlacByType(type), Vector2I.Zero, 0);
		}
	}

	public Vector2I FindNearestResource(Vector2 from, int radius, string kind, Rect2 area)
	{
		bool bounded = area.HasArea();
		Vector2I center = new Vector2I((int)from.X, (int)from.Y) / GenerationSettings.TILE_SIZE;

		for (int ring = 0; ring <= radius; ring++)
		{
			Vector2I best = new Vector2I(-1, -1);
			int bestDistance = int.MaxValue;

			for (int x = -ring; x <= ring; x++)
			{
				for (int y = -ring; y <= ring; y++)
				{
					if (Math.Abs(x) != ring && Math.Abs(y) != ring) continue;

					Vector2I cell = center + new Vector2I(x, y);
					if (bounded && !area.HasPoint((cell * GenerationSettings.TILE_SIZE) + Vector2.One * GenerationSettings.TILE_SIZE / 2)) continue;

					ResorseType type = GetResourceAt(cell);
					if (type == ResorseType.None || GameManager.KindOf(type) != kind) continue;

					int distance = x * x + y * y;
					if (distance < bestDistance)
					{
						bestDistance = distance;
						best = cell;
					}
				}
			}

			if (best.X >= 0) return best;
		}
		return new Vector2I(-1, -1);
	}

	public float CastResourceRay(Vector2 from, Vector2 direction, float length, string kind)
	{
		Vector2 position = from / GenerationSettings.TILE_SIZE;
		Vector2 dir = direction.Normalized();
		Vector2I cell = new Vector2I(Mathf.FloorToInt(position.X), Mathf.FloorToInt(position.Y));
		Vector2I step = new Vector2I(dir.X > 0 ? 1 : -1, dir.Y > 0 ? 1 : -1);
		float deltaX = dir.X == 0 ? float.MaxValue : Mathf.Abs(1f / dir.X);
		float deltaY = dir.Y == 0 ? float.MaxValue : Mathf.Abs(1f / dir.Y);
		float nextX = dir.X == 0 ? float.MaxValue : (dir.X > 0 ? cell.X + 1 - position.X : position.X - cell.X) * deltaX;
		float nextY = dir.Y == 0 ? float.MaxValue : (dir.Y > 0 ? cell.Y + 1 - position.Y : position.Y - cell.Y) * deltaY;
		float maxDistance = length / GenerationSettings.TILE_SIZE;

		while (true)
		{
			float distance;
			if (nextX < nextY)
			{
				cell.X += step.X;
				distance = nextX;
				nextX += deltaX;
			}
			else
			{
				cell.Y += step.Y;
				distance = nextY;
				nextY += deltaY;
			}
			if (distance > maxDistance) return -1;

			ResorseType type = GetResourceAt(cell);
			if (type != ResorseType.None && GameManager.KindOf(type) == kind) return distance * GenerationSettings.TILE_SIZE;
		}
	}

	public Vector2I lastcell = Vector2I.Zero;
	public void		GetCell()
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
