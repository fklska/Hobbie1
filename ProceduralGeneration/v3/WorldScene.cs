using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class WorldScene : Node2D
{
	[Signal] public delegate void TileHarvestedEventHandler(Vector2I cell, ResorseType type);

	[Export] public string WorldPath;

	public const int ObjectsZ = 2;
	public const int LoadRadius = 4;
	public const int KeepRadius = 5;
	public const double FrameBudgetMs = 3;

	public WorldMap Map { get; private set; }
	public string WorldName = "";
	public int WorldSeed;

	public TileMapLayer Ground;
	public TileMapLayer Edges;
	public TileMapLayer EnviromentLayer;
	public Node2D Enviroment;

	public Node2D FocusTarget;
	public double LoadBudgetMs;

	private TerrainRules rules;
	private Vector2 focus;
	private Vector2I focusChunk = new(int.MinValue, int.MinValue);
	private readonly HashSet<Vector2I> loaded = new();
	private readonly List<Vector2I> pending = new();
	private readonly List<Vector2I> stale = new();

	public Vector2I MapSize => Map?.Size ?? Vector2I.Zero;

	public override void _EnterTree() => GetTree().ProcessFrame += Stream;

	public override void _ExitTree() => GetTree().ProcessFrame -= Stream;

	public override void _Ready()
	{
		Ground = GetNode<TileMapLayer>("Terrain/Ground");
		Edges = GetNode<TileMapLayer>("Terrain/Edges");
		EnviromentLayer = GetNode<TileMapLayer>("EnviromentLayer");
		Enviroment = GetNode<Node2D>("Enviroment");
		YSortEnabled = true;
		EnviromentLayer.YSortEnabled = true;
		EnviromentLayer.ZIndex = ObjectsZ;
		Enviroment.YSortEnabled = true;
		Enviroment.ZIndex = 0;

		Map = WorldMap.Load(WorldPath);
		if (Map == null)
		{
			GD.PushError($"Не удалось прочитать мир {WorldPath}");
			return;
		}
		WorldName = Map.Name;
		WorldSeed = Map.Seed;
		GenerationSettings.MapSize = Map.Size;
		GenerationSettings.RecalculateSetting();
		SetUpWater();
		SetUpResources();
		focus = SafeSpawn(Map.SpawnPosition);
	}

	public Vector2 SpawnPosition() => Map == null ? Vector2.Zero : SafeSpawn(Map.SpawnPosition);

	public void FocusOn(Vector2 position) => focus = position;

	public void LoadAround(Vector2 position)
	{
		rules ??= TerrainRules.For(Ground.TileSet);
		if (Map == null || rules == null) return;
		focus = position;
		Vector2I center = ChunkOf(position);
		Refocus(center);
		for (int y = center.Y - 1; y <= center.Y + 1; y++)
			for (int x = center.X - 1; x <= center.X + 1; x++)
				DrawChunk(new Vector2I(x, y));
	}

	public float LoadedAround(Vector2 at, int distance)
	{
		if (Map == null) return 1f;
		Vector2I center = ChunkOf(at);
		int total = 0, done = 0;
		for (int y = center.Y - distance; y <= center.Y + distance; y++)
		{
			for (int x = center.X - distance; x <= center.X + distance; x++)
			{
				var chunk = new Vector2I(x, y);
				if (!HasChunk(chunk)) continue;
				total++;
				if (loaded.Contains(chunk)) done++;
			}
		}
		return total == 0 ? 1f : (float)done / total;
	}

	private static Vector2I ChunkOf(Vector2 position)
	{
		const float size = GenerationSettings.CHUNK_SIZE * GenerationSettings.TILE_SIZE;
		return new Vector2I(Mathf.FloorToInt(position.X / size), Mathf.FloorToInt(position.Y / size));
	}

	private bool HasChunk(Vector2I chunk) => chunk.X >= 0 && chunk.Y >= 0 && chunk.X < Map.Chunks.X && chunk.Y < Map.Chunks.Y;

	private static bool Near(Vector2I a, Vector2I b, int radius) => Math.Abs(a.X - b.X) <= radius && Math.Abs(a.Y - b.Y) <= radius;

	private void Stream()
	{
		if (Map == null) return;
		rules ??= TerrainRules.For(Ground.TileSet);
		if (rules == null) return;
		if (IsInstanceValid(FocusTarget)) focus = FocusTarget.GlobalPosition;
		Vector2I center = ChunkOf(focus);
		if (center != focusChunk) Refocus(center);
		if (pending.Count == 0 && stale.Count == 0) return;

		ulong start = Time.GetTicksUsec();
		ulong budget = (ulong)((LoadBudgetMs > 0 ? LoadBudgetMs : FrameBudgetMs) * 1000);
		while (pending.Count > 0 && Time.GetTicksUsec() - start < budget)
		{
			Vector2I chunk = pending[^1];
			pending.RemoveAt(pending.Count - 1);
			if (Near(chunk, focusChunk, LoadRadius)) DrawChunk(chunk);
		}
		while (stale.Count > 0 && Time.GetTicksUsec() - start < budget)
		{
			Vector2I chunk = stale[^1];
			stale.RemoveAt(stale.Count - 1);
			if (!Near(chunk, focusChunk, KeepRadius)) EraseChunk(chunk);
		}
	}

	private void Refocus(Vector2I center)
	{
		focusChunk = center;
		pending.Clear();
		for (int y = center.Y - LoadRadius; y <= center.Y + LoadRadius; y++)
		{
			for (int x = center.X - LoadRadius; x <= center.X + LoadRadius; x++)
			{
				var chunk = new Vector2I(x, y);
				if (HasChunk(chunk) && !loaded.Contains(chunk)) pending.Add(chunk);
			}
		}
		pending.Sort((a, b) => (b - center).LengthSquared().CompareTo((a - center).LengthSquared()));
		stale.Clear();
		foreach (Vector2I chunk in loaded)
			if (!Near(chunk, center, KeepRadius)) stale.Add(chunk);
	}

	private int TerrainAt(int x, int y) => Map.InBounds(x, y) ? rules.TerrainOf[Map.Biomes[y * Map.Width + x]] : -1;

	private (int x0, int y0, int x1, int y1) CellsOf(Vector2I chunk)
	{
		int x0 = chunk.X * GenerationSettings.CHUNK_SIZE, y0 = chunk.Y * GenerationSettings.CHUNK_SIZE;
		return (x0, y0, Math.Min(x0 + GenerationSettings.CHUNK_SIZE, Map.Width), Math.Min(y0 + GenerationSettings.CHUNK_SIZE, Map.Height));
	}

	private void DrawChunk(Vector2I chunk)
	{
		if (!HasChunk(chunk) || !loaded.Add(chunk)) return;
		(int x0, int y0, int x1, int y1) = CellsOf(chunk);
		for (int y = y0; y < y1; y++)
		{
			for (int x = x0; x < x1; x++)
			{
				int i = y * Map.Width + x;
				int source = rules.SourceOf[Map.Biomes[i]];
				if (source >= 0) Ground.SetCell(new Vector2I(x, y), source, TerrainRules.BaseTile);
				if (Map.Resources[i] != 0) DrawResource(new Vector2I(x, y));
			}
		}

		int ex = x1 == Map.Width ? x1 + 1 : x1, ey = y1 == Map.Height ? y1 + 1 : y1;
		for (int y = y0; y < ey; y++)
		{
			for (int x = x0; x < ex; x++)
			{
				int tile = rules.Edge(TerrainAt(x - 1, y - 1), TerrainAt(x, y - 1), TerrainAt(x - 1, y), TerrainAt(x, y));
				if (tile < 0) continue;
				Vector2I coords = TerrainRules.CoordsOf(tile);
				if (coords == TerrainRules.BaseTile) continue;
				Edges.SetCell(new Vector2I(x, y), TerrainRules.SourceOfTile(tile), coords);
			}
		}
	}

	private void EraseChunk(Vector2I chunk)
	{
		if (!loaded.Remove(chunk)) return;
		(int x0, int y0, int x1, int y1) = CellsOf(chunk);
		for (int y = y0; y < y1; y++)
		{
			for (int x = x0; x < x1; x++)
			{
				Ground.EraseCell(new Vector2I(x, y));
				if (Map.Resources[y * Map.Width + x] != 0) EnviromentLayer.EraseCell(new Vector2I(x, y));
			}
		}
		int ex = x1 == Map.Width ? x1 + 1 : x1, ey = y1 == Map.Height ? y1 + 1 : y1;
		for (int y = y0; y < ey; y++)
			for (int x = x0; x < ex; x++)
				Edges.EraseCell(new Vector2I(x, y));
	}

	public TileType GetBiomeAt(Vector2I cell) => Map?.BiomeAt(cell) ?? TileType.None;

	public ResorseType GetResourceAt(Vector2I cell) => Map?.ResourceAt(cell) ?? ResorseType.None;

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
}
