using System;
using System.Collections.Generic;

public static class WorldResources
{
	public const int Stages = 4;

	public record Info(string Title, string Look, string Kind, int Yield, int Portions, int Size);

	public static readonly Dictionary<ResorseType, Info> All = new()
	{
		[ResorseType.SmallWood] = new("Молодой дуб", "oak_small", "wood", 1, 4, 1),
		[ResorseType.MediumWood] = new("Дуб", "oak", "wood", 2, 5, 2),
		[ResorseType.GiantWood] = new("Вековой дуб", "oak_giant", "wood", 3, 6, 3),
		[ResorseType.Birch] = new("Берёза", "birch", "wood", 2, 5, 2),
		[ResorseType.SmallPine] = new("Молодая сосна", "pine_small", "wood", 1, 4, 1),
		[ResorseType.MediumPine] = new("Сосна", "pine", "wood", 2, 5, 2),
		[ResorseType.GiantPine] = new("Корабельная сосна", "pine_giant", "wood", 3, 6, 3),
		[ResorseType.SmallSnowWood] = new("Молодая ель", "fir_small", "wood", 1, 4, 1),
		[ResorseType.MediumSnowWood] = new("Ель", "fir", "wood", 2, 5, 2),
		[ResorseType.GiantSnowWood] = new("Старая ель", "fir_giant", "wood", 3, 6, 3),
		[ResorseType.PalmWood] = new("Пальма", "palm", "wood", 2, 5, 2),
		[ResorseType.TropicWood] = new("Тропическое дерево", "jungle", "wood", 3, 6, 3),
		[ResorseType.Banana] = new("Банан", "banana", "wood", 1, 4, 1),
		[ResorseType.Willow] = new("Ива", "willow", "wood", 2, 5, 2),
		[ResorseType.DeadTree] = new("Сухое дерево", "deadtree", "wood", 2, 4, 2),
		[ResorseType.Acacia] = new("Акация", "acacia", "wood", 2, 5, 2),
		[ResorseType.Baobab] = new("Баобаб", "baobab", "wood", 3, 7, 3),
		[ResorseType.Cactus] = new("Кактус", "cactus", "wood", 1, 3, 1),
		[ResorseType.Shrub] = new("Куст", "shrub", "wood", 1, 3, 1),
		[ResorseType.Stone] = new("Камень", "stone", "stone", 2, 8, 0),
		[ResorseType.Iron] = new("Железная руда", "iron", "iron", 1, 8, 0),
		[ResorseType.Gold] = new("Золотая жила", "gold", "gold", 1, 6, 0),
	};

	private static readonly Dictionary<TileType, (ResorseType Type, float Weight)[]> Flora = new()
	{
		[TileType.RegularForest] = new[] { (ResorseType.MediumWood, 0.34f), (ResorseType.Birch, 0.24f), (ResorseType.GiantWood, 0.18f), (ResorseType.SmallWood, 0.14f), (ResorseType.Shrub, 0.1f) },
		[TileType.Taiga] = new[] { (ResorseType.MediumPine, 0.4f), (ResorseType.GiantPine, 0.3f), (ResorseType.SmallPine, 0.18f), (ResorseType.Birch, 0.12f) },
		[TileType.Tundra] = new[] { (ResorseType.MediumSnowWood, 0.4f), (ResorseType.SmallSnowWood, 0.35f), (ResorseType.DeadTree, 0.15f), (ResorseType.GiantSnowWood, 0.1f) },
		[TileType.Snow] = new[] { (ResorseType.MediumSnowWood, 0.5f), (ResorseType.SmallSnowWood, 0.4f), (ResorseType.GiantSnowWood, 0.1f) },
		[TileType.TropicalForest] = new[] { (ResorseType.TropicWood, 0.45f), (ResorseType.Banana, 0.3f), (ResorseType.PalmWood, 0.25f) },
		[TileType.Swamp] = new[] { (ResorseType.Willow, 0.5f), (ResorseType.DeadTree, 0.25f), (ResorseType.Shrub, 0.25f) },
		[TileType.Savanna] = new[] { (ResorseType.Acacia, 0.55f), (ResorseType.Shrub, 0.3f), (ResorseType.Baobab, 0.15f) },
		[TileType.Desert] = new[] { (ResorseType.Cactus, 0.8f), (ResorseType.PalmWood, 0.2f) },
	};

	public static Info Of(ResorseType type) => All.GetValueOrDefault(type);

	public static string KindOf(ResorseType type) => Of(type)?.Kind ?? "";

	public static int YieldOf(ResorseType type) => Of(type)?.Yield ?? 0;

	public static int PortionsOf(ResorseType type) => Of(type)?.Portions ?? 0;

	public static bool IsTree(ResorseType type) => (Of(type)?.Size ?? 0) > 0;

	public static int HeightOf(ResorseType type) => Math.Max(0, (Of(type)?.Size ?? 0) - 1);

	public static int StageOf(ResorseType type, int taken)
	{
		int portions = PortionsOf(type);
		if (taken <= 0 || portions <= 1) return 0;
		return Math.Min(Stages - 1, (taken * (Stages - 1) + portions - 2) / (portions - 1));
	}

	public static string RockLook(TileType biome) => biome switch
	{
		TileType.Snow or TileType.Tundra => "snow",
		TileType.Desert or TileType.Savanna => "sand",
		TileType.Swamp or TileType.TropicalForest => "moss",
		_ => "granite",
	};

	public static string LookOf(ResorseType type, TileType biome)
	{
		Info info = Of(type);
		if (info == null) return "";
		return info.Size == 0 ? $"{info.Look}_{RockLook(biome)}" : info.Look;
	}

	public static bool HasFlora(TileType biome) => Flora.ContainsKey(biome);

	public static ResorseType TreeOf(TileType biome, double r) =>
		Flora.TryGetValue(biome, out var flora) ? Pick(flora, r, _ => true) : ResorseType.None;

	public static ResorseType Fit(ResorseType type, TileType biome, double r)
	{
		Info info = Of(type);
		if (info == null || info.Size == 0 || !Flora.TryGetValue(biome, out var flora)) return type;
		int best = int.MaxValue;
		foreach (var (tree, _) in flora)
		{
			if (tree == type) return type;
			best = Math.Min(best, Math.Abs(All[tree].Size - info.Size));
		}
		return Pick(flora, r, tree => Math.Abs(All[tree].Size - info.Size) == best);
	}

	public static double Hash(int x, int y)
	{
		uint h = (uint)(x * 374761393 + y * 668265263);
		h = (h ^ (h >> 13)) * 1274126177;
		return ((h ^ (h >> 16)) & 0xFFFFFF) / (double)0x1000000;
	}

	private static ResorseType Pick((ResorseType Type, float Weight)[] flora, double r, Func<ResorseType, bool> allowed)
	{
		float total = 0;
		foreach (var (tree, weight) in flora)
			if (allowed(tree)) total += weight;
		double left = r * total;
		ResorseType last = ResorseType.None;
		foreach (var (tree, weight) in flora)
		{
			if (!allowed(tree)) continue;
			last = tree;
			left -= weight;
			if (left < 0) return tree;
		}
		return last;
	}
}
