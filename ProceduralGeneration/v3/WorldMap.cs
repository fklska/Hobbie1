using Godot;
using System;

public class WorldMap
{
	public const string Extension = ".world";
	private const uint Magic = 0x31574248;
	private const uint Version = 1;

	public readonly int Width;
	public readonly int Height;
	public readonly byte[] Biomes;
	public readonly byte[] Resources;
	public readonly byte[] Elevation;
	public string Name = "";
	public int Seed;
	public Vector2I Spawn;

	public WorldMap(int width, int height)
	{
		Width = width;
		Height = height;
		Biomes = new byte[width * height];
		Resources = new byte[width * height];
		Elevation = new byte[width * height];
	}

	public Vector2I Size => new(Width, Height);

	public Vector2I Chunks => new((Width + GenerationSettings.CHUNK_SIZE - 1) / GenerationSettings.CHUNK_SIZE, (Height + GenerationSettings.CHUNK_SIZE - 1) / GenerationSettings.CHUNK_SIZE);

	public Vector2 SpawnPosition => (Spawn * GenerationSettings.TILE_SIZE) + Vector2.One * GenerationSettings.TILE_SIZE / 2;

	public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

	public bool InBounds(Vector2I cell) => InBounds(cell.X, cell.Y);

	public TileType BiomeAt(Vector2I cell) => InBounds(cell) ? (TileType)Biomes[cell.Y * Width + cell.X] : TileType.None;

	public ResorseType ResourceAt(Vector2I cell) => InBounds(cell) ? (ResorseType)Resources[cell.Y * Width + cell.X] : ResorseType.None;

	public void SetResource(Vector2I cell, ResorseType type)
	{
		if (InBounds(cell)) Resources[cell.Y * Width + cell.X] = (byte)type;
	}

	public static bool IsWater(TileType type) => type == TileType.DeepWater || type == TileType.TropicWater;

	public Error Save(string path)
	{
		string temp = path + ".tmp";
		using (FileAccess file = FileAccess.OpenCompressed(temp, FileAccess.ModeFlags.Write, FileAccess.CompressionMode.Zstd))
		{
			if (file == null) return FileAccess.GetOpenError();
			file.Store32(Magic);
			file.Store32(Version);
			file.Store32((uint)Width);
			file.Store32((uint)Height);
			file.Store32((uint)Seed);
			file.Store32((uint)Spawn.X);
			file.Store32((uint)Spawn.Y);
			file.StorePascalString(Name);
			file.StoreBuffer(Biomes);
			file.StoreBuffer(Resources);
			file.StoreBuffer(Elevation);
		}
		if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
		return DirAccess.RenameAbsolute(temp, path);
	}

	public static WorldMap Load(string path)
	{
		using FileAccess file = FileAccess.OpenCompressed(path, FileAccess.ModeFlags.Read, FileAccess.CompressionMode.Zstd);
		if (file == null || file.Get32() != Magic || file.Get32() > Version) return null;
		int width = (int)file.Get32();
		int height = (int)file.Get32();
		if (width <= 0 || height <= 0 || width > 4096 || height > 4096) return null;
		var map = new WorldMap(width, height)
		{
			Seed = (int)file.Get32(),
			Spawn = new Vector2I((int)file.Get32(), (int)file.Get32()),
			Name = file.GetPascalString(),
		};
		if (!Read(file, map.Biomes) || !Read(file, map.Resources) || !Read(file, map.Elevation)) return null;
		return map;
	}

	private static bool Read(FileAccess file, byte[] target)
	{
		byte[] data = file.GetBuffer(target.Length);
		if (data.Length != target.Length) return false;
		Buffer.BlockCopy(data, 0, target, 0, data.Length);
		return true;
	}

	public static WorldMap FromLegacy(GeneratorData data)
	{
		var map = new WorldMap(data.mapSize.X, data.mapSize.Y) { Name = data.WorldName ?? "", Seed = data.seed };
		foreach (var row in data.ChunkMap)
		{
			foreach (ChunkData chunk in row)
			{
				for (int x = 0; x < GenerationSettings.CHUNK_SIZE; x++)
				{
					var column = chunk.Map[x];
					for (int y = 0; y < GenerationSettings.CHUNK_SIZE; y++)
					{
						int gx = chunk.rect.X + x, gy = chunk.rect.Y + y;
						if (!map.InBounds(gx, gy)) continue;
						Tile tile = column[y];
						int i = gy * map.Width + gx;
						map.Biomes[i] = (byte)tile.Type;
						map.Resources[i] = (byte)tile.Resourse;
						map.Elevation[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(tile.heightValue * 255f), 0, 255);
					}
				}
			}
		}
		map.Spawn = SpawnFinder.Find(map.Width, map.Height, map.Biomes, map.Size / 2);
		return map;
	}

	public Image Preview()
	{
		var bytes = new byte[Width * Height * 3];
		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				int i = y * Width + x;
				TileType type = (TileType)Biomes[i];
				Color color = type == TileType.None ? Colors.Black : GenerationUtils.getTileTypeColor(type);
				if (!IsWater(type) && x > 0 && y > 0)
				{
					float slope = (Elevation[i] - Elevation[i - Width - 1]) / 255f;
					color = slope > 0 ? color.Lightened(Mathf.Min(slope * 6f, 0.25f)) : color.Darkened(Mathf.Min(-slope * 6f, 0.25f));
				}
				if (Resources[i] != 0) color = color.Darkened(0.25f);
				bytes[i * 3] = (byte)color.R8;
				bytes[i * 3 + 1] = (byte)color.G8;
				bytes[i * 3 + 2] = (byte)color.B8;
			}
		}
		return Image.CreateFromData(Width, Height, false, Image.Format.Rgb8, bytes);
	}
}
