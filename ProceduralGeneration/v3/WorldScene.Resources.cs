using Godot;
using System.Collections.Generic;

public partial class WorldScene
{
	private readonly Dictionary<Vector2I, int> taken = new();
	private readonly Dictionary<string, int> lookSources = new();

	private void SetUpResources()
	{
		TileSet set = EnviromentLayer.TileSet;
		for (int i = 0; i < set.GetSourceCount(); i++)
		{
			int id = set.GetSourceId(i);
			lookSources[set.GetSource(id).ResourceName] = id;
		}
		for (int i = 0; i < Map.Resources.Length; i++)
		{
			if (Map.Resources[i] == 0) continue;
			var type = (ResorseType)Map.Resources[i];
			Map.Resources[i] = (byte)WorldResources.Fit(type, (TileType)Map.Biomes[i], WorldResources.Hash(i % Map.Width, i / Map.Width));
		}
		AddChild(new ResourceHint { Name = "ResourceHint", World = this });
	}

	private void DrawResource(Vector2I cell)
	{
		ResorseType type = Map.ResourceAt(cell);
		if (type == ResorseType.None || !lookSources.TryGetValue(WorldResources.LookOf(type, Map.BiomeAt(cell)), out int source))
		{
			EnviromentLayer.EraseCell(cell);
			return;
		}
		EnviromentLayer.SetCell(cell, source, new Vector2I(0, WorldResources.StageOf(type, taken.GetValueOrDefault(cell))));
	}

	private void Redraw(Vector2I cell)
	{
		if (loaded.Contains(cell / GenerationSettings.CHUNK_SIZE)) DrawResource(cell);
	}

	public int StockAt(Vector2I cell)
	{
		ResorseType type = GetResourceAt(cell);
		return (WorldResources.PortionsOf(type) - taken.GetValueOrDefault(cell)) * WorldResources.YieldOf(type);
	}

	public Vector2I ResourceCellAt(Vector2 point)
	{
		var cell = new Vector2I(Mathf.FloorToInt(point.X / GenerationSettings.TILE_SIZE), Mathf.FloorToInt(point.Y / GenerationSettings.TILE_SIZE));
		if (GetResourceAt(cell) != ResorseType.None) return cell;
		for (int dy = 1; dy <= 2; dy++)
		{
			Vector2I below = cell + new Vector2I(0, dy);
			if (WorldResources.HeightOf(GetResourceAt(below)) >= dy) return below;
		}
		return cell;
	}

	public ResorseType HarvestTile(Vector2I cell) => Take(cell, taken.GetValueOrDefault(cell) + 1);

	public ResorseType ClearTile(Vector2I cell) => Take(cell, WorldResources.PortionsOf(GetResourceAt(cell)));

	private ResorseType Take(Vector2I cell, int count)
	{
		ResorseType type = GetResourceAt(cell);
		if (type == ResorseType.None) return ResorseType.None;
		SetTaken(cell, type, count);
		EmitSignal(SignalName.TileHarvested, cell, (int)type);
		return type;
	}

	private void SetTaken(Vector2I cell, ResorseType type, int count)
	{
		int portions = WorldResources.PortionsOf(type);
		taken[cell] = Mathf.Min(count, portions);
		if (count >= portions) Map.SetResource(cell, ResorseType.None);
		Redraw(cell);
	}

	public void RestoreTile(Vector2I cell, ResorseType type)
	{
		if (Map == null || !Map.InBounds(cell)) return;
		Map.SetResource(cell, type);
		taken.Remove(cell);
		Redraw(cell);
	}

	public Godot.Collections.Array CaptureHarvest()
	{
		var data = new Godot.Collections.Array();
		foreach (var (cell, count) in taken) data.Add(new Godot.Collections.Array { cell.X, cell.Y, count });
		return data;
	}

	public void RestoreHarvest(Godot.Collections.Array data)
	{
		foreach (Variant entry in data)
		{
			var xy = entry.AsGodotArray();
			var cell = new Vector2I(xy[0].AsInt32(), xy[1].AsInt32());
			ResorseType type = GetResourceAt(cell);
			if (type != ResorseType.None) SetTaken(cell, type, xy.Count > 2 ? xy[2].AsInt32() : WorldResources.PortionsOf(type));
		}
	}
}
