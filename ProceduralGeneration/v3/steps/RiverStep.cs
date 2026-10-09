using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class RiverStep : GenerationStep
{
	[Export] public int RiverThreshold = 160;
	[Export] public int WideRiverThreshold = 1200;
	[Export] public int MinRiverLength = 10;
	[Export] public float LakeDepth = 0.012f;
	[Export] public int MinLake = 10;
	[Export] public float MeanderFrequency = 0.09f;
	[Export] public float Meander = 0.012f;

	public override void Execute(WorldGen gen)
	{
		if (!Enabled) return;
		int w = gen.Width;
		int count = gen.Count;
		var filled = new float[count];
		var down = new int[count];
		var visited = new bool[count];
		var order = new List<int>(count);
		var queue = new PriorityQueue<int, float>();
		Array.Fill(down, -1);
		float[] wiggle = gen.Sample(gen.Noise("meander", MeanderFrequency, 2));
		var route = new float[count];
		for (int i = 0; i < count; i++) route[i] = gen.Elevation[i] + wiggle[i] * Meander;

		for (int i = 0; i < count; i++)
		{
			if (gen.Surfaces[i] != WorldGen.Surface.Ocean) continue;
			visited[i] = true;
			filled[i] = route[i];
			queue.Enqueue(i, filled[i]);
		}
		while (queue.TryDequeue(out int c, out float level))
		{
			order.Add(c);
			int x = c % w, y = c / w;
			for (int k = 0; k < 4; k++)
			{
				int nx = x + WorldGen.Dx[k], ny = y + WorldGen.Dy[k];
				if (!gen.InBounds(nx, ny)) continue;
				int j = ny * w + nx;
				if (visited[j]) continue;
				visited[j] = true;
				filled[j] = Math.Max(route[j], level + 1e-6f);
				down[j] = c;
				queue.Enqueue(j, filled[j]);
			}
		}

		MarkLakes(gen, filled, route);

		var flow = new int[count];
		for (int k = order.Count - 1; k >= 0; k--)
		{
			int c = order[k];
			if (gen.Surfaces[c] == WorldGen.Surface.Ocean) continue;
			flow[c]++;
			if (down[c] >= 0) flow[down[c]] += flow[c];
		}

		var river = new bool[count];
		for (int i = 0; i < count; i++) river[i] = gen.Surfaces[i] == WorldGen.Surface.Land && flow[i] >= RiverThreshold;

		var length = new int[count];
		for (int k = order.Count - 1; k >= 0; k--)
		{
			int c = order[k];
			if (!river[c]) continue;
			length[c] = Math.Max(length[c], 1);
			int d = down[c];
			if (d >= 0 && river[d]) length[d] = Math.Max(length[d], length[c] + 1);
		}
		var system = new int[count];
		var keep = new Dictionary<int, bool>();
		foreach (int c in order)
		{
			if (!river[c]) continue;
			int d = down[c];
			system[c] = d >= 0 && river[d] ? system[d] : c;
			if (system[c] == c) keep[c] = length[c] >= MinRiverLength;
		}

		for (int i = 0; i < count; i++)
		{
			if (!river[i] || !keep[system[i]]) continue;
			gen.Surfaces[i] = WorldGen.Surface.River;
			if (flow[i] < WideRiverThreshold) continue;
			int x = i % w, y = i / w;
			int side = down[i] >= 0 && Math.Abs(down[i] - i) == 1 ? (y + 1 < gen.Height ? i + w : -1) : (x + 1 < w ? i + 1 : -1);
			if (side >= 0 && gen.Surfaces[side] == WorldGen.Surface.Land) gen.Surfaces[side] = WorldGen.Surface.River;
		}
	}

	private void MarkLakes(WorldGen gen, float[] filled, float[] route)
	{
		int w = gen.Width;
		var seen = new bool[gen.Count];
		var queue = new Queue<int>();
		var members = new List<int>();
		for (int start = 0; start < gen.Count; start++)
		{
			if (seen[start] || !IsBasin(gen, filled, route, start)) continue;
			members.Clear();
			seen[start] = true;
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				int i = queue.Dequeue();
				members.Add(i);
				int x = i % w, y = i / w;
				for (int k = 0; k < 4; k++)
				{
					int nx = x + WorldGen.Dx[k], ny = y + WorldGen.Dy[k];
					if (!gen.InBounds(nx, ny)) continue;
					int j = ny * w + nx;
					if (seen[j] || !IsBasin(gen, filled, route, j)) continue;
					seen[j] = true;
					queue.Enqueue(j);
				}
			}
			if (members.Count < MinLake) continue;
			foreach (int i in members) gen.Surfaces[i] = WorldGen.Surface.Lake;
		}
	}

	private bool IsBasin(WorldGen gen, float[] filled, float[] route, int i) =>
		gen.Surfaces[i] == WorldGen.Surface.Land && filled[i] - route[i] > LakeDepth;
}
