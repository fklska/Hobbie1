using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class CoastStep : GenerationStep
{
	[Export] public float ShallowWidth = 4f;
	[Export] public float ShallowExtra = 3f;
	[Export] public int MinDeepWidth = 5;
	[Export] public int BeachWidth = 1;
	[Export] public int BeachExtra = 1;
	[Export] public float Frequency = 0.06f;

	public override void Execute(WorldGen gen)
	{
		if (!Enabled) return;
		FastNoiseLite noise = gen.Noise("coast", Frequency, 2);
		float[] fromLand = gen.SmoothDistance(i => gen.Surfaces[i] != WorldGen.Surface.Ocean);
		int[] fromOcean = gen.Distance(i => gen.Surfaces[i] == WorldGen.Surface.Ocean, true, BeachWidth + BeachExtra);
		var deep = new bool[gen.Count];

		for (int i = 0; i < gen.Count; i++)
		{
			float n = Mathf.Clamp(noise.GetNoise2D(i % gen.Width, i / gen.Width) * 1.5f + 0.5f, 0f, 1f);
			if (gen.Surfaces[i] == WorldGen.Surface.Ocean)
			{
				deep[i] = fromLand[i] > ShallowWidth + ShallowExtra * n;
			}
			else if (gen.Surfaces[i] == WorldGen.Surface.Land && fromOcean[i] <= BeachWidth + Mathf.RoundToInt(BeachExtra * n))
			{
				gen.Biomes[i] = (byte)TileType.Desert;
			}
		}

		deep = OpenSea(gen, Open(gen, deep));
		for (int i = 0; i < gen.Count; i++)
			if (gen.IsWater(i)) gen.Biomes[i] = (byte)(deep[i] ? TileType.DeepWater : TileType.TropicWater);
	}

	private bool[] Open(WorldGen gen, bool[] deep)
	{
		int radius = MinDeepWidth / 2;
		if (radius <= 0) return deep;
		int[] fromShallow = gen.Distance(i => !deep[i], true, radius + 1);
		int[] fromCore = gen.Distance(i => fromShallow[i] > radius, true, radius + 1);
		var open = new bool[gen.Count];
		for (int i = 0; i < gen.Count; i++) open[i] = deep[i] && fromCore[i] <= radius;
		return open;
	}

	private static bool[] OpenSea(WorldGen gen, bool[] deep)
	{
		int w = gen.Width, h = gen.Height;
		var sea = new bool[gen.Count];
		var queue = new Queue<int>();
		for (int i = 0; i < gen.Count; i++)
		{
			int x = i % w, y = i / w;
			if (!deep[i] || (x != 0 && y != 0 && x != w - 1 && y != h - 1)) continue;
			sea[i] = true;
			queue.Enqueue(i);
		}
		while (queue.Count > 0)
		{
			int i = queue.Dequeue();
			int x = i % w, y = i / w;
			for (int k = 0; k < 4; k++)
			{
				int nx = x + WorldGen.Dx[k], ny = y + WorldGen.Dy[k];
				if (!gen.InBounds(nx, ny)) continue;
				int j = ny * w + nx;
				if (!deep[j] || sea[j]) continue;
				sea[j] = true;
				queue.Enqueue(j);
			}
		}
		return sea;
	}
}
