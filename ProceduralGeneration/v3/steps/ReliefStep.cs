using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[GlobalClass]
public partial class ReliefStep : GenerationStep
{
	public const float Sea = 0.4f;

	[Export] public float Frequency = 0.012f;
	[Export] public int Octaves = 5;
	[Export] public float WarpFrequency = 0.015f;
	[Export] public float WarpAmplitude = 14f;
	[Export] public float RidgeFrequency = 0.018f;
	[Export] public float RidgeWeight = 0.35f;
	[Export] public float IslandWeight = 0.9f;
	[Export] public float IslandStart = 0.35f;
	[Export(PropertyHint.Range, "0.2,0.8")] public float LandRatio = 0.45f;
	[Export] public int OceanMargin = 10;
	[Export] public int MinLake = 6;

	public override void Execute(WorldGen gen)
	{
		if (!Enabled) return;
		int w = gen.Width, h = gen.Height;
		FastNoiseLite relief = gen.Noise("relief", Frequency, Octaves);
		FastNoiseLite warpX = gen.Noise("warp-x", WarpFrequency, 3);
		FastNoiseLite warpY = gen.Noise("warp-y", WarpFrequency, 3);
		FastNoiseLite ridge = gen.Noise("ridge", RidgeFrequency, 4, FastNoiseLite.FractalTypeEnum.Ridged);
		float[] raw = new float[gen.Count];

		Parallel.For(0, h, y =>
		{
			for (int x = 0; x < w; x++)
			{
				float wx = x + warpX.GetNoise2D(x, y) * WarpAmplitude;
				float wy = y + warpY.GetNoise2D(x, y) * WarpAmplitude;
				float nx = 2f * x / (w - 1) - 1f, ny = 2f * y / (h - 1) - 1f;
				float d = 1f - (1f - nx * nx) * (1f - ny * ny);
				float island = 1f - Mathf.SmoothStep(IslandStart, 1f, d);
				float r = (ridge.GetNoise2D(wx, wy) + 1f) * 0.5f;
				raw[y * w + x] = relief.GetNoise2D(wx, wy) + (island - 0.5f) * IslandWeight + r * r * RidgeWeight * island;
			}
		});

		float[] sorted = (float[])raw.Clone();
		Array.Sort(sorted);
		float sea = sorted[Mathf.Clamp((int)((1f - LandRatio) * sorted.Length), 0, sorted.Length - 1)];
		float min = sorted[0], max = sorted[^1];

		for (int i = 0; i < gen.Count; i++)
		{
			float e = raw[i] < sea
				? Sea * (raw[i] - min) / Mathf.Max(sea - min, 1e-6f)
				: Sea + (1f - Sea) * (raw[i] - sea) / Mathf.Max(max - sea, 1e-6f);
			int x = i % w, y = i / w;
			int edge = Math.Min(Math.Min(x, y), Math.Min(w - 1 - x, h - 1 - y));
			if (edge < OceanMargin) e = Mathf.Min(e, Sea * 0.98f * edge / OceanMargin);
			gen.Elevation[i] = e;
		}
		gen.SeaLevel = Sea;

		ClassifyWater(gen);
		RankLand(gen);
	}

	private void ClassifyWater(WorldGen gen)
	{
		int w = gen.Width, h = gen.Height;
		var label = new int[gen.Count];
		var queue = new Queue<int>();
		var members = new List<int>();
		for (int start = 0; start < gen.Count; start++)
		{
			if (gen.Elevation[start] >= Sea || label[start] != 0) continue;
			members.Clear();
			bool ocean = false;
			label[start] = 1;
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				int i = queue.Dequeue();
				members.Add(i);
				int x = i % w, y = i / w;
				if (x == 0 || y == 0 || x == w - 1 || y == h - 1) ocean = true;
				for (int k = 0; k < 4; k++)
				{
					int nx = x + WorldGen.Dx[k], ny = y + WorldGen.Dy[k];
					if (!gen.InBounds(nx, ny)) continue;
					int j = ny * w + nx;
					if (gen.Elevation[j] >= Sea || label[j] != 0) continue;
					label[j] = 1;
					queue.Enqueue(j);
				}
			}
			foreach (int i in members)
			{
				if (ocean) gen.Surfaces[i] = WorldGen.Surface.Ocean;
				else if (members.Count >= MinLake) gen.Surfaces[i] = WorldGen.Surface.Lake;
				else gen.Elevation[i] = Sea + 0.001f;
			}
		}
	}

	private static void RankLand(WorldGen gen)
	{
		var land = new List<int>();
		for (int i = 0; i < gen.Count; i++)
			if (gen.Surfaces[i] == WorldGen.Surface.Land) land.Add(i);
		int[] cells = land.ToArray();
		float[] keys = new float[cells.Length];
		for (int k = 0; k < cells.Length; k++) keys[k] = gen.Elevation[cells[k]];
		Array.Sort(keys, cells);
		for (int k = 0; k < cells.Length; k++) gen.LandHeight[cells[k]] = cells.Length > 1 ? (float)k / (cells.Length - 1) : 0f;
	}
}
