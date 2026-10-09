using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WorldGen
{
	public enum Surface : byte { Land, Ocean, Lake, River }

	public static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
	public static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

	public readonly int Width;
	public readonly int Height;
	public readonly int Seed;
	public readonly float[] Elevation;
	public readonly float[] LandHeight;
	public readonly float[] Temperature;
	public readonly float[] Moisture;
	public readonly Surface[] Surfaces;
	public readonly byte[] Biomes;
	public readonly byte[] Resources;
	public float SeaLevel;
	public Vector2I Spawn = new(-1, -1);

	public WorldGen(int width, int height, int seed)
	{
		Width = width;
		Height = height;
		Seed = seed;
		int count = width * height;
		Elevation = new float[count];
		LandHeight = new float[count];
		Temperature = new float[count];
		Moisture = new float[count];
		Surfaces = new Surface[count];
		Biomes = new byte[count];
		Resources = new byte[count];
	}

	public int Count => Width * Height;

	public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

	public TileType Biome(int i) => (TileType)Biomes[i];

	public bool IsWater(int i) => Surfaces[i] != Surface.Land;

	public int SeedFor(string key)
	{
		unchecked
		{
			uint hash = 2166136261;
			foreach (char c in key)
			{
				hash ^= c;
				hash *= 16777619;
			}
			hash ^= (uint)Seed;
			hash *= 16777619;
			hash ^= hash >> 15;
			hash *= 2246822519;
			hash ^= hash >> 13;
			return (int)(hash & 0x7fffffff);
		}
	}

	public Random Random(string key) => new(SeedFor(key));

	public FastNoiseLite Noise(string key, float frequency, int octaves, FastNoiseLite.FractalTypeEnum fractal = FastNoiseLite.FractalTypeEnum.Fbm)
	{
		return new FastNoiseLite
		{
			Seed = SeedFor(key),
			NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
			Frequency = frequency,
			FractalType = fractal,
			FractalOctaves = octaves,
		};
	}

	public float[] Sample(FastNoiseLite noise)
	{
		var values = new float[Count];
		Parallel.For(0, Height, y =>
		{
			for (int x = 0; x < Width; x++) values[y * Width + x] = noise.GetNoise2D(x, y);
		});
		return values;
	}

	public int[] Distance(Func<int, bool> source, bool diagonal, int limit = int.MaxValue)
	{
		var distance = new int[Count];
		var queue = new Queue<int>();
		for (int i = 0; i < Count; i++)
		{
			if (source(i))
			{
				distance[i] = 0;
				queue.Enqueue(i);
			}
			else distance[i] = int.MaxValue;
		}
		int directions = diagonal ? 8 : 4;
		while (queue.Count > 0)
		{
			int i = queue.Dequeue();
			if (distance[i] >= limit) continue;
			int x = i % Width, y = i / Width;
			for (int k = 0; k < directions; k++)
			{
				int nx = x + Dx[k], ny = y + Dy[k];
				if (!InBounds(nx, ny)) continue;
				int j = ny * Width + nx;
				if (distance[j] != int.MaxValue) continue;
				distance[j] = distance[i] + 1;
				queue.Enqueue(j);
			}
		}
		return distance;
	}

	public float[] SmoothDistance(Func<int, bool> source)
	{
		const float Diagonal = 1.41421356f;
		var distance = new float[Count];
		for (int i = 0; i < Count; i++) distance[i] = source(i) ? 0 : float.MaxValue;
		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				int i = y * Width + x;
				float d = distance[i];
				if (x > 0) d = Math.Min(d, distance[i - 1] + 1);
				if (y > 0)
				{
					d = Math.Min(d, distance[i - Width] + 1);
					if (x > 0) d = Math.Min(d, distance[i - Width - 1] + Diagonal);
					if (x < Width - 1) d = Math.Min(d, distance[i - Width + 1] + Diagonal);
				}
				distance[i] = d;
			}
		}
		for (int y = Height - 1; y >= 0; y--)
		{
			for (int x = Width - 1; x >= 0; x--)
			{
				int i = y * Width + x;
				float d = distance[i];
				if (x < Width - 1) d = Math.Min(d, distance[i + 1] + 1);
				if (y < Height - 1)
				{
					d = Math.Min(d, distance[i + Width] + 1);
					if (x < Width - 1) d = Math.Min(d, distance[i + Width + 1] + Diagonal);
					if (x > 0) d = Math.Min(d, distance[i + Width - 1] + Diagonal);
				}
				distance[i] = d;
			}
		}
		return distance;
	}

	public WorldMap ToMap(string name)
	{
		var map = new WorldMap(Width, Height) { Name = name, Seed = Seed, Spawn = Spawn };
		Buffer.BlockCopy(Biomes, 0, map.Biomes, 0, Count);
		Buffer.BlockCopy(Resources, 0, map.Resources, 0, Count);
		for (int i = 0; i < Count; i++) map.Elevation[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(Elevation[i] * 255f), 0, 255);
		return map;
	}

	public Image Layer(float[] values, Gradient gradient)
	{
		var bytes = new byte[Count * 3];
		for (int i = 0; i < Count; i++)
		{
			Color color = gradient?.Sample(Mathf.Clamp(values[i], 0f, 1f)) ?? new Color(values[i], values[i], values[i]);
			if (Surfaces[i] == Surface.Ocean && values != Elevation) color = color.Darkened(0.6f);
			bytes[i * 3] = (byte)color.R8;
			bytes[i * 3 + 1] = (byte)color.G8;
			bytes[i * 3 + 2] = (byte)color.B8;
		}
		return Image.CreateFromData(Width, Height, false, Image.Format.Rgb8, bytes);
	}
}
