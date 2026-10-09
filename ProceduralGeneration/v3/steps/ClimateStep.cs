using Godot;
using System;
using System.Threading.Tasks;

[GlobalClass]
public partial class ClimateStep : GenerationStep
{
	[Export] public float PoleTemperature = 0.12f;
	[Export] public float EquatorTemperature = 0.95f;
	[Export] public float HeatFrequency = 0.01f;
	[Export] public float HeatNoise = 0.3f;
	[Export] public float Lapse = 0.35f;
	[Export] public float MoistureFrequency = 0.014f;
	[Export] public float MoistureNoise = 0.9f;
	[Export] public float WaterMoisture = 0.25f;
	[Export] public float WaterReach = 6f;
	[Export] public float MountainLevel = 0.93f;
	[Export] public float PeakLevel = 0.985f;
	[Export] public int SmoothPasses = 2;

	public override void Execute(WorldGen gen)
	{
		if (!Enabled) return;
		int w = gen.Width, h = gen.Height;
		FastNoiseLite heat = gen.Noise("heat", HeatFrequency, 3);
		FastNoiseLite moisture = gen.Noise("moisture", MoistureFrequency, 4);
		float[] waterDistance = gen.SmoothDistance(gen.IsWater);
		(int north, int south) = LandRows(gen);

		Parallel.For(0, h, y =>
		{
			float latitude = Mathf.Lerp(PoleTemperature, EquatorTemperature, Mathf.Clamp((float)(y - north) / Math.Max(south - north, 1), 0f, 1f));
			for (int x = 0; x < w; x++)
			{
				int i = y * w + x;
				float lift = gen.Surfaces[i] == WorldGen.Surface.Land ? gen.LandHeight[i] : 0f;
				gen.Temperature[i] = Mathf.Clamp(latitude + heat.GetNoise2D(x, y) * HeatNoise - lift * lift * lift * Lapse, 0f, 1f);
				float wet = WaterMoisture * Mathf.Exp(-waterDistance[i] / WaterReach);
				gen.Moisture[i] = Mathf.Clamp(0.5f + moisture.GetNoise2D(x, y) * MoistureNoise + wet, 0f, 1f);
				gen.Biomes[i] = (byte)Pick(gen, i, waterDistance[i]);
			}
		});

		for (int pass = 0; pass < SmoothPasses; pass++) Smooth(gen);
	}

	private static (int north, int south) LandRows(WorldGen gen)
	{
		int north = gen.Height, south = 0;
		for (int i = 0; i < gen.Count; i++)
		{
			if (gen.Surfaces[i] != WorldGen.Surface.Land) continue;
			int y = i / gen.Width;
			north = Math.Min(north, y);
			south = Math.Max(south, y);
		}
		return north > south ? (0, gen.Height - 1) : (north, south);
	}

	private TileType Pick(WorldGen gen, int i, float waterDistance)
	{
		switch (gen.Surfaces[i])
		{
			case WorldGen.Surface.Ocean: return TileType.DeepWater;
			case WorldGen.Surface.Lake:
			case WorldGen.Surface.River: return TileType.TropicWater;
		}
		float t = gen.Temperature[i], m = gen.Moisture[i], lift = gen.LandHeight[i];
		if (lift >= PeakLevel || (lift >= MountainLevel && t < 0.2f)) return TileType.Snow;
		if (lift >= MountainLevel) return TileType.StoneMountain;
		if (t < 0.2f) return m > 0.55f ? TileType.Snow : TileType.Tundra;
		if (t < 0.38f) return m > 0.42f ? TileType.Taiga : TileType.Tundra;
		if (lift < 0.35f && waterDistance <= 5f && m > 0.72f && t < 0.85f) return TileType.Swamp;
		if (t < 0.66f) return m > 0.36f ? TileType.RegularForest : TileType.Savanna;
		if (m > 0.6f) return TileType.TropicalForest;
		return m > 0.34f ? TileType.Savanna : TileType.Desert;
	}

	private static void Smooth(WorldGen gen)
	{
		int w = gen.Width, h = gen.Height;
		byte[] source = (byte[])gen.Biomes.Clone();
		Parallel.For(1, h - 1, y =>
		{
			Span<int> counts = stackalloc int[32];
			for (int x = 1; x < w - 1; x++)
			{
				int i = y * w + x;
				TileType own = (TileType)source[i];
				if (gen.Surfaces[i] != WorldGen.Surface.Land || own == TileType.StoneMountain || own == TileType.Snow) continue;
				counts.Clear();
				for (int dy = -1; dy <= 1; dy++)
					for (int dx = -1; dx <= 1; dx++)
					{
						int j = i + dy * w + dx;
						if (gen.Surfaces[j] == WorldGen.Surface.Land) counts[source[j]]++;
					}
				if (counts[source[i]] >= 4) continue;
				int best = source[i];
				for (int b = 0; b < counts.Length; b++)
				{
					TileType type = (TileType)b;
					if (type == TileType.StoneMountain || type == TileType.Snow) continue;
					if (counts[b] > counts[best]) best = b;
				}
				gen.Biomes[i] = (byte)best;
			}
		});
	}
}
