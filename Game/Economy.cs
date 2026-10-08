using Godot;
using System.Collections.Generic;
using System.Linq;

public enum WeaponKind { Melee, Ranged }

public static class Economy
{
	public record Level(string Title, Dictionary<string, int> Cost, int Hp, int CoreLevel,
		int Pop = 0, int Storage = 0, int Food = 0, int Militia = 0, int Damage = 0, float Range = 0,
		int Tax = 0, float Sell = 0, float Buy = 0, string[] Requires = null);

	public record BuildingInfo(string ScenePath, Vector2I Footprint, int[] MaxCount, string Description, Level[] Levels, bool Repeat = false);

	public record WeaponInfo(string Title, WeaponKind Kind, int Damage, float Range, float Cooldown, float ProjectileSpeed,
		Dictionary<string, int> Cost, string Building, int BuildingLevel, string Icon);

	public record Tier(string Title, Dictionary<string, int> Cost, float Value, int ForgeLevel);

	public const string Coins = "coins";
	public const string Core = "TownHall";
	public const int TradeLot = 10;
	public const int FoodPerWorker = 1;

	public static readonly string[] Resources = { "wood", "stone", "iron", "gold", "food", Coins };
	public static readonly string[] Gatherable = { "wood", "stone", "iron", "gold" };
	public static readonly string[] Tradable = { "wood", "stone", "iron", "gold", "food" };

	public static readonly Dictionary<string, string> ResourceTitles = new()
	{
		["wood"] = "Дерево",
		["stone"] = "Камень",
		["iron"] = "Железо",
		["gold"] = "Золото",
		["food"] = "Еда",
		[Coins] = "Монеты",
	};

	public static readonly Dictionary<string, int> Prices = new()
	{
		["wood"] = 1,
		["stone"] = 1,
		["iron"] = 3,
		["gold"] = 8,
		["food"] = 1,
	};

	public static readonly Dictionary<string, int> StartStock = C(("wood", 10), ("food", 15));
	public static readonly Dictionary<string, int> HireCost = C(("food", 6));

	public static readonly string[] BuildOrder = { "House", "Farm", "Storage", "Blacksmith", "Workshop", "Market", "Barracks", "Wall", "Tower" };

	public static readonly Dictionary<string, BuildingInfo> Buildings = new()
	{
		[Core] = new("res://BuildSystem/buildings/TownHall.tscn", new(2, 2), new[] { 1, 1, 1, 1 },
			"Сердце поселения. Улучшение открывает новые постройки", new Level[]
			{
				new("Костёр", C(), 300, 1, Pop: 2, Storage: 60, Militia: 1),
				new("Деревянная ратуша", C(("wood", 50), ("stone", 30)), 600, 1, Pop: 3, Storage: 120, Militia: 2,
					Requires: new[] { "House", "Farm" }),
				new("Каменная ратуша", C(("wood", 150), ("stone", 150), ("iron", 40), (Coins, 60)), 1000, 2, Pop: 4, Storage: 200, Militia: 3,
					Requires: new[] { "Storage", "Blacksmith", "Market" }),
				new("Замок", C(("wood", 250), ("stone", 300), ("iron", 100), ("gold", 30), (Coins, 200)), 1600, 3, Pop: 5, Storage: 300, Militia: 4,
					Requires: new[] { "Workshop", "Barracks" }),
			}),
		["House"] = new("res://BuildSystem/buildings/house.tscn", new(1, 1), new[] { 1, 2, 3, 4 },
			"Жильё для жителей", new Level[]
			{
				new("Шалаш", C(("wood", 20)), 120, 1, Pop: 2),
				new("Дом", C(("wood", 30), ("stone", 15)), 250, 2, Pop: 3),
				new("Каменный дом", C(("wood", 30), ("stone", 40), ("iron", 5)), 450, 3, Pop: 4),
			}),
		["Farm"] = new("res://BuildSystem/buildings/farm.tscn", new(2, 2), new[] { 1, 2, 3, 4 },
			"Приносит еду каждое утро", new Level[]
			{
				new("Огород", C(("wood", 25)), 120, 1, Food: 4),
				new("Ферма", C(("wood", 40), ("stone", 20)), 250, 2, Food: 7),
				new("Большая ферма", C(("wood", 60), ("stone", 50), ("iron", 10)), 400, 3, Food: 10),
			}),
		["Storage"] = new("res://BuildSystem/buildings/storage.tscn", new(2, 2), new[] { 1, 1, 2, 3 },
			"Увеличивает запас ресурсов", new Level[]
			{
				new("Навес", C(("wood", 30)), 150, 1, Storage: 100),
				new("Амбар", C(("wood", 60), ("stone", 30)), 300, 2, Storage: 200),
				new("Каменный склад", C(("wood", 60), ("stone", 100), ("iron", 15)), 600, 3, Storage: 350),
			}),
		["Blacksmith"] = new("res://BuildSystem/buildings/black_smith.tscn", new(3, 2), new[] { 1, 1, 1, 1 },
			"Инструменты, доспехи и оружие ближнего боя", new Level[]
			{
				new("Горн", C(("wood", 40), ("stone", 20)), 300, 1),
				new("Кузница", C(("wood", 60), ("stone", 60), ("iron", 20)), 500, 2),
				new("Оружейная кузня", C(("wood", 80), ("stone", 120), ("iron", 50), (Coins, 60)), 800, 3),
			}),
		["Workshop"] = new("res://BuildSystem/buildings/workshop.tscn", new(2, 2), new[] { 0, 1, 1, 1 },
			"Оружие дальнего боя", new Level[]
			{
				new("Мастерская лучника", C(("wood", 50), ("stone", 20)), 250, 2),
				new("Мастерская арбалетчика", C(("wood", 80), ("stone", 40), ("iron", 20)), 400, 3),
				new("Оружейная мастерская", C(("wood", 100), ("stone", 80), ("iron", 40), ("gold", 10), (Coins, 80)), 600, 4),
			}),
		["Market"] = new("res://BuildSystem/buildings/market.tscn", new(2, 2), new[] { 0, 1, 1, 1 },
			"Торговля и налоги с жителей каждое утро", new Level[]
			{
				new("Меновой двор", C(("wood", 50), ("stone", 30)), 250, 2, Tax: 1, Sell: 0.5f, Buy: 2f),
				new("Рынок", C(("wood", 80), ("stone", 60), (Coins, 40)), 400, 3, Tax: 2, Sell: 0.65f, Buy: 1.7f),
				new("Торговая палата", C(("wood", 120), ("stone", 120), ("gold", 10), (Coins, 100)), 600, 4, Tax: 3, Sell: 0.8f, Buy: 1.4f),
			}),
		["Barracks"] = new("res://BuildSystem/buildings/barracks.tscn", new(2, 2), new[] { 0, 1, 1, 1 },
			"Позволяет вооружить больше жителей", new Level[]
			{
				new("Сторожка", C(("wood", 60), ("stone", 40)), 350, 2, Militia: 3),
				new("Казарма", C(("wood", 80), ("stone", 80), ("iron", 20)), 600, 3, Militia: 6),
				new("Гарнизон", C(("wood", 100), ("stone", 150), ("iron", 40), (Coins, 80)), 900, 4, Militia: 10),
			}),
		["Wall"] = new("res://BuildSystem/buildings/wall.tscn", new(1, 1), new[] { 20, 40, 60, 80 },
			"Задерживает врагов. Жители и герой проходят насквозь", new Level[]
			{
				new("Частокол", C(("wood", 4)), 150, 1),
				new("Каменная стена", C(("stone", 5)), 350, 2),
				new("Крепостная стена", C(("stone", 8), ("iron", 1)), 700, 3),
			}, Repeat: true),
		["Tower"] = new("res://BuildSystem/buildings/tower.tscn", new(1, 1), new[] { 0, 2, 4, 6 },
			"Стреляет по врагам", new Level[]
			{
				new("Деревянная вышка", C(("wood", 30), ("stone", 10)), 200, 2, Damage: 6, Range: 360),
				new("Сторожевая башня", C(("wood", 30), ("stone", 40)), 400, 3, Damage: 10, Range: 420),
				new("Каменная башня", C(("stone", 60), ("iron", 20)), 700, 4, Damage: 16, Range: 480),
			}),
	};

	public static readonly string[] MeleeWeapons = { "club", "spear", "sword", "halberd" };
	public static readonly string[] RangedWeapons = { "sling", "bow", "crossbow", "musket" };
	public const string StartHeroWeapon = "club";

	public static readonly Dictionary<string, WeaponInfo> Weapons = new()
	{
		["club"] = new("Дубина", WeaponKind.Melee, 10, 60, 0.9f, 0, C(("wood", 5)), "Blacksmith", 1, "res://Items/Assets/weapons/club.png"),
		["spear"] = new("Копьё", WeaponKind.Melee, 15, 90, 1.0f, 0, C(("wood", 6), ("iron", 3)), "Blacksmith", 1, "res://Items/Assets/weapons/spear.png"),
		["sword"] = new("Меч", WeaponKind.Melee, 24, 70, 0.7f, 0, C(("wood", 2), ("iron", 8), (Coins, 10)), "Blacksmith", 2, "res://Items/Assets/weapons/sword.png"),
		["halberd"] = new("Алебарда", WeaponKind.Melee, 36, 100, 1.1f, 0, C(("wood", 6), ("iron", 12), ("gold", 1), (Coins, 25)), "Blacksmith", 3, "res://Items/Assets/weapons/halberd.png"),
		["sling"] = new("Праща", WeaponKind.Ranged, 6, 300, 1.2f, 600, C(("wood", 3), ("food", 2)), "Workshop", 1, "res://Items/Assets/weapons/sling.png"),
		["bow"] = new("Лук", WeaponKind.Ranged, 10, 420, 1.0f, 800, C(("wood", 8), (Coins, 5)), "Workshop", 1, "res://Items/Assets/weapons/bow.png"),
		["crossbow"] = new("Арбалет", WeaponKind.Ranged, 18, 460, 1.6f, 1000, C(("wood", 8), ("iron", 5), (Coins, 15)), "Workshop", 2, "res://Items/Assets/weapons/crossbow.png"),
		["musket"] = new("Мушкет", WeaponKind.Ranged, 40, 520, 2.8f, 1600, C(("wood", 5), ("iron", 10), ("gold", 2), (Coins, 30)), "Workshop", 3, "res://Items/Assets/weapons/musket.png"),
	};

	public static readonly Tier[] Tools =
	{
		new("Каменные инструменты", C(), 1f, 0),
		new("Железные инструменты", C(("wood", 20), ("iron", 15)), 1.4f, 1),
		new("Стальные инструменты", C(("wood", 30), ("iron", 40), (Coins, 30)), 1.8f, 2),
		new("Закалённые инструменты", C(("iron", 60), ("gold", 10), (Coins, 80)), 2.3f, 3),
	};

	public static readonly Tier[] Armor =
	{
		new("Без доспеха", C(), 100, 0),
		new("Кожаный доспех", C(("wood", 10), ("food", 15)), 130, 1),
		new("Кольчуга", C(("iron", 30), (Coins, 30)), 170, 2),
		new("Латы", C(("iron", 70), ("gold", 5), (Coins, 80)), 230, 3),
	};

	public static Dictionary<string, int> C(params (string kind, int amount)[] items) => items.ToDictionary(i => i.kind, i => i.amount);

	public static string CostText(Dictionary<string, int> cost, int times = 1) => cost.Count == 0
		? "бесплатно"
		: string.Join(", ", cost.Select(c => $"{ResourceTitles[c.Key].ToLower()} {c.Value * times}"));

	public static string EffectText(Level level)
	{
		List<string> parts = new();
		if (level.Pop > 0) parts.Add($"жители +{level.Pop}");
		if (level.Storage > 0) parts.Add($"склад +{level.Storage}");
		if (level.Food > 0) parts.Add($"еда +{level.Food} в день");
		if (level.Militia > 0) parts.Add($"ополчение +{level.Militia}");
		if (level.Damage > 0) parts.Add($"урон {level.Damage}, дальность {level.Range}");
		if (level.Tax > 0) parts.Add($"налог {level.Tax} с жителя");
		parts.Add($"прочность {level.Hp}");
		return string.Join(", ", parts);
	}

	public static string WeaponText(WeaponInfo w) =>
		$"урон {w.Damage}, дальность {w.Range}, перезарядка {w.Cooldown} с";
}
