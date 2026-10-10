using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class ResourceStep : GenerationStep
{
	[Export] public float ForestFrequency = 0.035f;
	[Export] public float ForestLevel = 0f;
	[Export] public int ClearRadius = 4;
	[Export] public int StartRadius = 10;
	[Export] public int StartTrees = 12;
	[Export] public int StartStone = 5;
	[Export] public int IronRadius = 22;
	[Export] public int GoldRadius = 40;
	[Export] public float StoneClusters = 1f / 260f;
	[Export] public float IronClusters = 1f / 520f;
	[Export] public float GoldClusters = 1f / 1400f;
	[Export] public int ClusterSpacing = 7;
	[Export] public int OasisRadius = 3;
	[Export] public float OasisDensity = 0.3f;

	public override void Execute(WorldGen gen)
	{
		if (!Enabled) return;
		gen.Spawn = SpawnFinder.Find(gen.Width, gen.Height, gen.Biomes, new Vector2I(gen.Width / 2, gen.Height / 2));
		var placer = new Placer(this, gen);
		placer.Trees();
		placer.Mountains();
		placer.Ores();
		placer.Start();
	}

	private class Placer
	{
		private readonly ResourceStep step;
		private readonly WorldGen gen;
		private readonly Random rng;
		private readonly int[] reachable;
		private readonly int[] fromOcean;
		private readonly int[] fromWater;
		private readonly List<int> centers = new();
		private readonly int landCells;

		public Placer(ResourceStep step, WorldGen gen)
		{
			this.step = step;
			this.gen = gen;
			rng = gen.Random("resources");
			var blocked = new bool[gen.Count];
			for (int i = 0; i < gen.Count; i++)
			{
				blocked[i] = gen.IsWater(i);
				if (!blocked[i]) landCells++;
			}
			reachable = SpawnFinder.LargestComponent(gen.Width, gen.Height, blocked);
			fromOcean = gen.Distance(i => gen.Surfaces[i] == WorldGen.Surface.Ocean, true, 3);
			fromWater = gen.Distance(i => gen.IsWater(i), true, step.OasisRadius + 1);
		}

		private int Ring(int i) => Math.Max(Math.Abs(i % gen.Width - gen.Spawn.X), Math.Abs(i / gen.Width - gen.Spawn.Y));

		private bool Free(int i) =>
			gen.Surfaces[i] == WorldGen.Surface.Land && gen.Resources[i] == 0 && gen.Biome(i) != TileType.None
			&& fromOcean[i] > 2 && Ring(i) > step.ClearRadius;

		private bool Spaced(int i)
		{
			int x = i % gen.Width, y = i / gen.Width;
			for (int k = 0; k < 8; k++)
			{
				int nx = x + WorldGen.Dx[k], ny = y + WorldGen.Dy[k];
				if (gen.InBounds(nx, ny) && IsTree((ResorseType)gen.Resources[ny * gen.Width + nx])) return false;
			}
			return true;
		}

		private static bool IsTree(ResorseType type) => type != ResorseType.None && GameManager.KindOf(type) == "wood";

		private int[] Shuffled()
		{
			var order = new int[gen.Count];
			for (int i = 0; i < order.Length; i++) order[i] = i;
			for (int i = order.Length - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(order[i], order[j]) = (order[j], order[i]);
			}
			return order;
		}

		public void Trees()
		{
			float[] forest = gen.Sample(gen.Noise("forest", step.ForestFrequency, 3));
			foreach (int i in Shuffled())
			{
				if (!Free(i)) continue;
				TileType biome = gen.Biome(i);
				bool oasis = biome == TileType.Desert && fromWater[i] <= step.OasisRadius;
				(float patch, float open) = oasis ? (step.OasisDensity, step.OasisDensity) : TreeDensity(biome);
				float density = forest[i] > step.ForestLevel ? patch : open;
				if (density <= 0 || rng.NextDouble() >= density || !Spaced(i)) continue;
				gen.Resources[i] = (byte)(oasis ? ResorseType.PalmWood : TreeOf(biome));
			}
		}

		public void Mountains()
		{
			for (int i = 0; i < gen.Count; i++)
			{
				if (!Free(i)) continue;
				TileType biome = gen.Biome(i);
				if (biome != TileType.StoneMountain && biome != TileType.Snow) continue;
				double r = rng.NextDouble() * (biome == TileType.StoneMountain ? 1.0 : 2.5);
				if (r < 0.1) gen.Resources[i] = (byte)ResorseType.Stone;
				else if (r < 0.14) gen.Resources[i] = (byte)ResorseType.Iron;
				else if (r < 0.15 && Ring(i) >= 20) gen.Resources[i] = (byte)ResorseType.Gold;
			}
		}

		public void Ores()
		{
			Clusters(ResorseType.Stone, Count(step.StoneClusters), 3, 7, 0, StoneWeight);
			Clusters(ResorseType.Iron, Count(step.IronClusters), 2, 5, 10, IronWeight);
			Clusters(ResorseType.Gold, Count(step.GoldClusters), 1, 3, 20, GoldWeight);
		}

		private int Count(float rate) => Math.Max(1, Mathf.RoundToInt(landCells * rate));

		private void Clusters(ResorseType kind, int count, int minSize, int maxSize, int minRing, Func<TileType, float> weight)
		{
			int placed = 0;
			foreach (int i in Shuffled())
			{
				if (placed >= count) break;
				if (!Free(i) || Ring(i) < minRing || rng.NextDouble() >= weight(gen.Biome(i)) || Crowded(i)) continue;
				Cluster(i, kind, rng.Next(minSize, maxSize + 1));
				placed++;
			}
		}

		private bool Crowded(int i)
		{
			int x = i % gen.Width, y = i / gen.Width;
			foreach (int c in centers)
				if (Math.Max(Math.Abs(c % gen.Width - x), Math.Abs(c / gen.Width - y)) < step.ClusterSpacing) return true;
			return false;
		}

		private int Cluster(int center, ResorseType kind, int size)
		{
			centers.Add(center);
			int x = center % gen.Width, y = center / gen.Width;
			int placed = 0;
			for (int attempt = 0; attempt < size * 8 && placed < size; attempt++)
			{
				int nx = x + (attempt == 0 ? 0 : rng.Next(-2, 3));
				int ny = y + (attempt == 0 ? 0 : rng.Next(-2, 3));
				if (!gen.InBounds(nx, ny)) continue;
				int j = ny * gen.Width + nx;
				if (!Free(j)) continue;
				gen.Resources[j] = (byte)kind;
				placed++;
			}
			return placed;
		}

		private int Grove(int center, int size)
		{
			int x = center % gen.Width, y = center / gen.Width;
			int placed = 0;
			for (int attempt = 0; attempt < size * 8 && placed < size; attempt++)
			{
				int nx = x + (attempt == 0 ? 0 : rng.Next(-3, 4));
				int ny = y + (attempt == 0 ? 0 : rng.Next(-3, 4));
				if (!gen.InBounds(nx, ny)) continue;
				int j = ny * gen.Width + nx;
				if (!Free(j) || !Spaced(j)) continue;
				TileType biome = gen.Biome(j);
				gen.Resources[j] = (byte)(TreeDensity(biome).patch > 0 ? TreeOf(biome) : TreeOf(TileType.RegularForest));
				placed++;
			}
			return placed;
		}

		public void Start()
		{
			Ensure(IsTree, step.StartTrees, step.ClearRadius + 2, step.StartRadius, i => Grove(i, step.StartTrees));
			Ensure(t => t == ResorseType.Stone, step.StartStone, step.ClearRadius + 2, step.StartRadius, i => Cluster(i, ResorseType.Stone, step.StartStone + 1));
			Ensure(t => t == ResorseType.Iron, 3, step.StartRadius + 2, step.IronRadius, i => Cluster(i, ResorseType.Iron, 4));
			Ensure(t => t == ResorseType.Gold, 2, step.IronRadius + 2, step.GoldRadius, i => Cluster(i, ResorseType.Gold, 3));
		}

		private void Ensure(Func<ResorseType, bool> match, int need, int minRing, int maxRing, Func<int, int> place)
		{
			for (int round = 0; round < 3; round++)
			{
				int have = 0;
				var candidates = new List<int>();
				for (int y = gen.Spawn.Y - maxRing; y <= gen.Spawn.Y + maxRing; y++)
				{
					for (int x = gen.Spawn.X - maxRing; x <= gen.Spawn.X + maxRing; x++)
					{
						if (!gen.InBounds(x, y)) continue;
						int i = y * gen.Width + x;
						if (reachable[i] == 0) continue;
						if (match((ResorseType)gen.Resources[i])) have++;
						else if (Ring(i) >= minRing && Free(i)) candidates.Add(i);
					}
				}
				if (have >= need || candidates.Count == 0) return;
				place(candidates[rng.Next(candidates.Count)]);
			}
		}

		private static (float patch, float open) TreeDensity(TileType biome) => biome switch
		{
			TileType.RegularForest => (0.55f, 0.05f),
			TileType.Taiga => (0.6f, 0.08f),
			TileType.TropicalForest => (0.6f, 0.1f),
			TileType.Swamp => (0.25f, 0.06f),
			TileType.Savanna => (0.15f, 0.03f),
			TileType.Tundra => (0.1f, 0.02f),
			TileType.Snow => (0.05f, 0f),
			TileType.Desert => (0.05f, 0.01f),
			_ => (0f, 0f),
		};

		private ResorseType TreeOf(TileType biome) => WorldResources.TreeOf(biome, rng.NextDouble());

		private static float StoneWeight(TileType biome) => biome switch
		{
			TileType.StoneMountain => 1f,
			TileType.Snow or TileType.Tundra or TileType.Desert or TileType.Savanna => 0.45f,
			TileType.Taiga or TileType.RegularForest => 0.3f,
			TileType.TropicalForest => 0.15f,
			_ => 0.1f,
		};

		private static float IronWeight(TileType biome) => biome switch
		{
			TileType.StoneMountain => 1f,
			TileType.Snow => 0.6f,
			TileType.Tundra => 0.4f,
			TileType.Taiga => 0.25f,
			TileType.Savanna or TileType.Desert => 0.15f,
			_ => 0.08f,
		};

		private static float GoldWeight(TileType biome) => biome switch
		{
			TileType.StoneMountain => 0.8f,
			TileType.Desert => 0.5f,
			TileType.Snow => 0.3f,
			TileType.Savanna => 0.2f,
			TileType.Tundra => 0.15f,
			_ => 0.05f,
		};
	}
}
