using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager
{
	public class PlacedBuilding
	{
		public string Id;
		public Vector2I Cell;
		public Node2D Node;
	}

	public Dictionary<string, int> Levels = new();
	public List<PlacedBuilding> Placed = new();
	public Dictionary<string, int> Armory = new();
	public int ToolTier;
	public int ArmorTier;
	public string HeroWeapon = Economy.StartHeroWeapon;
	public bool Hungry;

	private readonly HashSet<Vector2I> occupiedCells = new();
	private readonly Dictionary<string, ulong> fullNoticeAt = new();

	public Node2D MainBase => IsInstanceValid(TownHall) ? TownHall : null;
	public int CoreLevel => Levels[Economy.Core];

	private void ResetEconomy()
	{
		foreach (string kind in Economy.Resources) Stock[kind] = Economy.StartStock.GetValueOrDefault(kind);
		foreach (string id in Economy.Buildings.Keys) Levels[id] = 1;
		foreach (string id in Economy.Weapons.Keys) Armory[id] = 0;
		Placed.Clear();
		occupiedCells.Clear();
		fullNoticeAt.Clear();
		ToolTier = 0;
		ArmorTier = 0;
		HeroWeapon = Economy.StartHeroWeapon;
		Hungry = false;
	}

	private void StartEconomy(Vector2 spawn)
	{
		if (!Restoring) SpawnBuilding(Economy.Core, FindFreeCell(Economy.Core, Grid.pixelToCell(spawn)));
		ConnectDayCycle();
		ApplyHeroStats();
	}

	private Vector2I FindFreeCell(string id, Vector2I near)
	{
		for (int radius = 2; radius < 24; radius++)
			for (int x = -radius; x <= radius; x++)
				for (int y = -radius; y <= radius; y++)
				{
					if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius) continue;
					Vector2I cell = near + new Vector2I(x, y);
					if (CanPlace(id, cell)) return cell;
				}
		return near + new Vector2I(2, 0);
	}

	private void ConnectDayCycle()
	{
		Node cycle = Player?.FindChildren("*", "", true, false).FirstOrDefault(n => n.HasSignal("day_started"));
		cycle?.Connect("day_started", Callable.From<int>(OnDayStarted));
	}

	public Economy.Level LevelInfo(string id) => Economy.Buildings[id].Levels[Levels[id] - 1];

	public Economy.Level NextLevel(string id)
	{
		Economy.Level[] levels = Economy.Buildings[id].Levels;
		return Levels[id] < levels.Length ? levels[Levels[id]] : null;
	}

	public int GetLevel(string id) => Levels.GetValueOrDefault(id);

	public int CountOf(string id) => Placed.Count(p => p.Id == id && IsInstanceValid(p.Node));

	public bool HasBuilding(string id) => CountOf(id) > 0;

	public int MaxCount(string id) => Economy.Buildings[id].MaxCount[CoreLevel - 1];

	public int Capacity(string kind)
	{
		if (kind == Economy.Coins) return int.MaxValue;
		return LevelInfo(Economy.Core).Storage + CountOf("Storage") * LevelInfo("Storage").Storage;
	}

	public int PopulationCap => LevelInfo(Economy.Core).Pop + CountOf("House") * LevelInfo("House").Pop;

	public int MilitiaCap => LevelInfo(Economy.Core).Militia + (HasBuilding("Barracks") ? LevelInfo("Barracks").Militia : 0);

	public int FoodPerDay => CountOf("Farm") * LevelInfo("Farm").Food;

	public int FoodUpkeep => Workers.Count * Economy.FoodPerWorker;

	public int TaxPerDay => HasBuilding("Market") ? Workers.Count * LevelInfo("Market").Tax : 0;

	public float ToolSpeed => Economy.Tools[ToolTier].Value;

	public float WorkSpeed => ToolSpeed * (Hungry ? 0.5f : 1f);

	public int HeroDamage => Economy.Weapons[HeroWeapon].Damage + 4 * ArtifactCount("fang");

	public int HeroMaxHealth => (int)Economy.Armor[ArmorTier].Value + 20 * ArtifactCount("stone_heart") + (ClassInfo?.BonusHealth ?? 0);

	private int ArtifactCount(string id) => IsInstanceValid(Survival) ? Survival.Count(id) : 0;

	public int GetStock(string kind) => Stock.GetValueOrDefault(kind);

	public int AddResource(string kind, int amount)
	{
		if (!Stock.ContainsKey(kind) || amount <= 0) return 0;
		int added = Mathf.Min(amount, Capacity(kind) - Stock[kind]);
		if (added < amount) NotifyFull(kind);
		if (added <= 0) return 0;
		Stock[kind] += added;
		EmitSignal(SignalName.StockChanged);
		return added;
	}

	public int AddCoins(int amount) => AddResource(Economy.Coins, amount);

	private void NotifyFull(string kind)
	{
		ulong now = Time.GetTicksMsec();
		if (now < fullNoticeAt.GetValueOrDefault(kind)) return;
		fullNoticeAt[kind] = now + 15000;
		Notify($"Склад полон: {Economy.ResourceTitles[kind].ToLower()}. Постройте или улучшите склад");
	}

	public void AddHarvest(ResorseType type) => AddResource(KindOf(type), YieldOf(type));

	public bool CanAfford(Dictionary<string, int> cost, int times = 1) => cost.All(c => GetStock(c.Key) >= c.Value * times);

	public bool TrySpend(Dictionary<string, int> cost, int times = 1)
	{
		if (!CanAfford(cost, times))
		{
			string missing = string.Join(", ", cost.Where(c => GetStock(c.Key) < c.Value * times)
				.Select(c => $"{Economy.ResourceTitles[c.Key].ToLower()} {c.Value * times - GetStock(c.Key)}"));
			SoundManager.Instance.Play("error");
			Notify($"Не хватает: {missing}");
			return false;
		}
		foreach (var c in cost) Stock[c.Key] -= c.Value * times;
		EmitSignal(SignalName.StockChanged);
		return true;
	}

	public string BuildLock(string id)
	{
		if (!IsInstanceValid(TownHall)) return "Нет центра поселения";
		Economy.Level level = LevelInfo(id);
		if (CoreLevel < level.CoreLevel) return $"Нужен центр: {Economy.Buildings[Economy.Core].Levels[level.CoreLevel - 1].Title}";
		int max = MaxCount(id);
		if (max == 0)
		{
			int need = System.Array.FindIndex(Economy.Buildings[id].MaxCount, m => m > 0);
			return $"Нужен центр: {Economy.Buildings[Economy.Core].Levels[need].Title}";
		}
		if (CountOf(id) >= max) return max == 1 ? "Уже построено" : $"Предел {max}, улучшите центр";
		return "";
	}

	public string UpgradeLock(string id)
	{
		Economy.Level next = NextLevel(id);
		if (next == null) return "Максимальный уровень";
		if (!HasBuilding(id)) return "Сначала постройте";
		if (CoreLevel < next.CoreLevel) return $"Нужен центр: {Economy.Buildings[Economy.Core].Levels[next.CoreLevel - 1].Title}";
		string[] missing = (next.Requires ?? System.Array.Empty<string>()).Where(r => !HasBuilding(r)).ToArray();
		if (missing.Length > 0) return "Нужно построить: " + string.Join(", ", missing.Select(r => LevelInfo(r).Title.ToLower()));
		return "";
	}

	public int UpgradeTimes(string id) => Mathf.Max(1, CountOf(id));

	public bool CanPlace(string id, Vector2I cell)
	{
		if (World == null) return false;
		Grid grid = GetNode<Grid>("/root/BuildMode");
		Vector2I footprint = Economy.Buildings[id].Footprint;
		for (int x = 0; x < footprint.X; x++)
			for (int y = 0; y < footprint.Y; y++)
			{
				Vector2I c = cell + new Vector2I(x, y);
				if (occupiedCells.Contains(c) || !grid.IsAbleToPlace(c)) return false;
			}
		return true;
	}

	public bool PlaceBuilding(string id, Vector2I cell)
	{
		if (Ended) return true;
		if (id == Economy.Core)
		{
			if (!IsInstanceValid(TownHall)) SpawnBuilding(id, CanPlace(id, cell) ? cell : FindFreeCell(id, cell));
			return true;
		}
		string lockReason = BuildLock(id);
		if (lockReason != "")
		{
			Notify(lockReason);
			return true;
		}
		if (!CanPlace(id, cell))
		{
			SoundManager.Instance.Play("error");
			Notify("Здесь строить нельзя");
			return false;
		}
		Economy.Level level = LevelInfo(id);
		if (!TrySpend(level.Cost)) return true;

		SoundManager.Instance.PlayAt("build", BuildingCenter(SpawnBuilding(id, cell)));
		Notify($"Построено: {level.Title}");
		return !Economy.Buildings[id].Repeat || BuildLock(id) != "" || !CanAfford(LevelInfo(id).Cost);
	}

	private Node2D SpawnBuilding(string id, Vector2I cell, int hp = 0)
	{
		Economy.BuildingInfo info = Economy.Buildings[id];
		Node2D node = GD.Load<PackedScene>(info.ScenePath).Instantiate<Node2D>();
		node.Position = cell * GenerationSettings.TILE_SIZE;
		EntitiesRoot.AddChild(node);
		node.Call("set_level", Levels[id], LevelInfo(id).Hp);
		if (hp > 0) node.Call("restore_hp", hp);

		PlacedBuilding placed = new() { Id = id, Cell = cell, Node = node };
		Placed.Add(placed);
		SetCells(placed, true);
		if (id == Economy.Core) TownHall = node;
		node.Connect("destroyed", Callable.From(() => OnBuildingDestroyed(placed)));
		EmitSignal(SignalName.ProgressChanged);
		return node;
	}

	private void SetCells(PlacedBuilding placed, bool occupied)
	{
		Vector2I footprint = Economy.Buildings[placed.Id].Footprint;
		for (int x = 0; x < footprint.X; x++)
			for (int y = 0; y < footprint.Y; y++)
			{
				Vector2I c = placed.Cell + new Vector2I(x, y);
				if (occupied) occupiedCells.Add(c);
				else occupiedCells.Remove(c);
			}
	}

	private void OnBuildingDestroyed(PlacedBuilding placed)
	{
		Placed.Remove(placed);
		SetCells(placed, false);
		string title = LevelInfo(placed.Id).Title;
		if (placed.Id == Economy.Core)
		{
			TownHall = null;
			EndGame(false, $"Поселение пало: {title.ToLower()} разрушен{(title.EndsWith("а") ? "а" : "")}");
			return;
		}
		Notify($"Разрушено: {title}");
		EmitSignal(SignalName.ProgressChanged);
		EmitSignal(SignalName.StockChanged);
	}

	public void UpgradeBuilding(string id)
	{
		if (Ended) return;
		string lockReason = UpgradeLock(id);
		if (lockReason != "")
		{
			Notify(lockReason);
			return;
		}
		Economy.Level next = NextLevel(id);
		if (!TrySpend(next.Cost, UpgradeTimes(id))) return;
		Levels[id]++;
		foreach (PlacedBuilding p in Placed.Where(p => p.Id == id && IsInstanceValid(p.Node)))
			p.Node.Call("set_level", Levels[id], next.Hp);
		SoundManager.Instance.Play("build");
		Notify($"Улучшено: {next.Title}");
		EmitSignal(SignalName.ProgressChanged);
		EmitSignal(SignalName.StockChanged);
	}

	public void HireWorker()
	{
		if (Ended) return;
		if (!IsInstanceValid(TownHall))
		{
			Notify("Нет центра поселения");
			return;
		}
		if (Workers.Count >= PopulationCap)
		{
			Notify("Нет места для жителей: постройте или улучшите жильё");
			return;
		}
		if (!TrySpend(Economy.HireCost)) return;

		Node2D worker = GD.Load<PackedScene>("res://AI/Village/worker.tscn").Instantiate<Node2D>();
		worker.Position = BuildingCenter(TownHall) + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 96f;
		EntitiesRoot.AddChild(worker);
		Workers.Add(worker);
		worker.TreeExiting += () => OnWorkerGone(worker);
		EmitSignal(SignalName.ProgressChanged);
	}

	private void OnWorkerGone(Node2D worker)
	{
		if (!Workers.Remove(worker)) return;
		EmitSignal(SignalName.ProgressChanged);
	}

	public void SetWorkerJob(string kind)
	{
		if (Economy.Gatherable.Contains(kind)) WorkerJob = kind;
	}

	private void OnDayStarted(int day)
	{
		if (Ended || !IsInstanceValid(TownHall)) return;
		int harvest = FoodPerDay;
		Stock["food"] = Mathf.Min(Capacity("food"), Stock["food"] + harvest);
		int upkeep = FoodUpkeep;
		Hungry = Stock["food"] < upkeep;
		Stock["food"] = Mathf.Max(0, Stock["food"] - upkeep);
		int tax = TaxPerDay;
		Stock[Economy.Coins] += tax;

		List<string> report = new() { $"День {day}" };
		if (harvest > 0) report.Add($"урожай +{harvest}");
		if (upkeep > 0) report.Add($"съедено {upkeep}");
		if (tax > 0) report.Add($"налоги +{tax}");
		Notify(string.Join(", ", report));
		if (Hungry) Notify("Жителям не хватило еды: сегодня они работают вдвое медленнее");
		EmitSignal(SignalName.StockChanged);
		EmitSignal(SignalName.ProgressChanged);
	}

	public string ToolLock() => TierLock(Economy.Tools, ToolTier);

	public string ArmorLock() => TierLock(Economy.Armor, ArmorTier);

	private string TierLock(Economy.Tier[] tiers, int current)
	{
		if (current + 1 >= tiers.Length) return "Лучшее уже есть";
		return ForgeLock(tiers[current + 1].ForgeLevel);
	}

	private string ForgeLock(int level) => BuildingLevelLock("Blacksmith", level);

	private string BuildingLevelLock(string id, int level)
	{
		if (!HasBuilding(id) || Levels[id] < level) return $"Нужно: {Economy.Buildings[id].Levels[level - 1].Title} (ур. {level})";
		return "";
	}

	public void UpgradeTools()
	{
		if (Ended || !TryUpgradeTier(Economy.Tools, ref ToolTier)) return;
		Notify($"Теперь у жителей и героя {Economy.Tools[ToolTier].Title.ToLower()}");
	}

	public void UpgradeArmor()
	{
		if (Ended || !TryUpgradeTier(Economy.Armor, ref ArmorTier)) return;
		ApplyHeroStats();
		Notify($"Герой надел: {Economy.Armor[ArmorTier].Title.ToLower()}");
	}

	private bool TryUpgradeTier(Economy.Tier[] tiers, ref int current)
	{
		string lockReason = TierLock(tiers, current);
		if (lockReason != "")
		{
			Notify(lockReason);
			return false;
		}
		if (!TrySpend(tiers[current + 1].Cost)) return false;
		current++;
		SoundManager.Instance.Play("forge");
		EmitSignal(SignalName.ProgressChanged);
		return true;
	}

	public void ApplyHeroStats()
	{
		if (IsInstanceValid(Player)) Player.SetMaxHealth(HeroMaxHealth);
	}

	public string WeaponLock(string id)
	{
		Economy.WeaponInfo weapon = Economy.Weapons[id];
		return BuildingLevelLock(weapon.Building, weapon.BuildingLevel);
	}

	public void ForgeWeapon(string id, int count = 1)
	{
		if (Ended) return;
		string lockReason = WeaponLock(id);
		if (lockReason != "")
		{
			Notify(lockReason);
			return;
		}
		if (!TrySpend(Economy.Weapons[id].Cost, count)) return;
		Armory[id] += count;
		SoundManager.Instance.Play("forge");
		Notify($"В арсенале: {Economy.Weapons[id].Title.ToLower()} ×{Armory[id]}");
		EmitSignal(SignalName.ProgressChanged);
	}

	public int ArmoryCount(string id) => Armory.GetValueOrDefault(id);

	public bool TakeWeapon(string id)
	{
		if (ArmoryCount(id) <= 0) return false;
		Armory[id]--;
		EmitSignal(SignalName.ProgressChanged);
		return true;
	}

	public void ReturnWeapon(string id)
	{
		if (!Armory.ContainsKey(id)) return;
		Armory[id]++;
		EmitSignal(SignalName.ProgressChanged);
	}

	public string BestWeapon(string kind)
	{
		string[] ids = kind == "ranged" ? Economy.RangedWeapons : Economy.MeleeWeapons;
		return ids.Where(id => ArmoryCount(id) > 0).OrderByDescending(id => Economy.Weapons[id].Damage).FirstOrDefault() ?? "";
	}

	public void EquipHero(string id)
	{
		if (Ended || id == HeroWeapon || Economy.Weapons[id].Kind != WeaponKind.Melee) return;
		if (!TakeWeapon(id))
		{
			Notify("Сначала выкуйте это оружие");
			return;
		}
		ReturnWeapon(HeroWeapon);
		HeroWeapon = id;
		Notify($"Герой взял: {Economy.Weapons[id].Title.ToLower()}");
	}

	public Godot.Collections.Dictionary GetWeaponStats(string id)
	{
		Economy.WeaponInfo w = Economy.Weapons[id];
		return new()
		{
			["id"] = id,
			["title"] = w.Title,
			["kind"] = w.Kind == WeaponKind.Ranged ? "ranged" : "melee",
			["tier"] = System.Array.IndexOf(w.Kind == WeaponKind.Ranged ? Economy.RangedWeapons : Economy.MeleeWeapons, id) + 1,
			["damage"] = w.Damage,
			["range"] = w.Range,
			["cooldown"] = w.Cooldown,
			["projectile_speed"] = w.ProjectileSpeed,
			["icon"] = w.Icon,
		};
	}

	public Godot.Collections.Dictionary GetLevelStats(string id)
	{
		Economy.Level level = LevelInfo(id);
		return new()
		{
			["id"] = id,
			["level"] = Levels[id],
			["title"] = level.Title,
			["hp"] = level.Hp,
			["damage"] = level.Damage,
			["range"] = level.Range,
		};
	}

	public void DisarmWorkers()
	{
		foreach (Node2D worker in Workers.Where(w => IsInstanceValid(w) && w.Get("weapon_id").AsString() != "").ToList())
			ArmWorker(worker, "");
	}

	public int SellPrice(string kind) => HasBuilding("Market")
		? Mathf.Max(1, Mathf.FloorToInt(Economy.Prices[kind] * Economy.TradeLot * LevelInfo("Market").Sell))
		: 0;

	public int BuyPrice(string kind) => HasBuilding("Market")
		? Mathf.CeilToInt(Economy.Prices[kind] * Economy.TradeLot * LevelInfo("Market").Buy)
		: 0;

	public void Sell(string kind)
	{
		if (Ended || !HasBuilding("Market")) return;
		if (!TrySpend(Economy.C((kind, Economy.TradeLot)))) return;
		AddCoins(SellPrice(kind));
		SoundManager.Instance.Play("coin");
	}

	public void Buy(string kind)
	{
		if (Ended || !HasBuilding("Market")) return;
		if (GetStock(kind) + Economy.TradeLot > Capacity(kind))
		{
			NotifyFull(kind);
			return;
		}
		if (!TrySpend(Economy.C((Economy.Coins, BuyPrice(kind))))) return;
		AddResource(kind, Economy.TradeLot);
		SoundManager.Instance.Play("coin");
	}

	public string EconomyObjective()
	{
		if (!IsInstanceValid(TownHall)) return "";
		if (Workers.Count == 0) return $"Наймите жителя во вкладке «Деревня» ({Economy.CostText(Economy.HireCost)})";
		if (!HasBuilding("Farm")) return $"Постройте огород, чтобы прокормить жителей ({Economy.CostText(LevelInfo("Farm").Cost)})";
		Economy.Level next = NextLevel(Economy.Core);
		if (next == null) return "Поселение достигло расцвета";
		string lockReason = UpgradeLock(Economy.Core);
		if (lockReason != "") return $"{lockReason}, чтобы перейти к «{next.Title}»";
		return $"Улучшите центр до «{next.Title}» ({Economy.CostText(next.Cost)})";
	}

	public Godot.Collections.Dictionary SaveEconomy()
	{
		Godot.Collections.Array buildings = new();
		foreach (PlacedBuilding p in Placed.Where(p => IsInstanceValid(p.Node)))
			buildings.Add(new Godot.Collections.Dictionary { ["id"] = p.Id, ["cell"] = new Godot.Collections.Array { p.Cell.X, p.Cell.Y }, ["hp"] = p.Node.Get("hp") });

		Godot.Collections.Dictionary levels = new();
		foreach (var l in Levels) levels[l.Key] = l.Value;
		Godot.Collections.Dictionary armory = new();
		foreach (var a in Armory) armory[a.Key] = a.Value;

		return new()
		{
			["levels"] = levels,
			["buildings"] = buildings,
			["armory"] = armory,
			["tool_tier"] = ToolTier,
			["armor_tier"] = ArmorTier,
			["hero_weapon"] = HeroWeapon,
			["worker_job"] = WorkerJob,
			["hungry"] = Hungry,
		};
	}

	public void LoadEconomy(Godot.Collections.Dictionary data)
	{
		foreach (PlacedBuilding p in Placed.ToList())
		{
			if (!IsInstanceValid(p.Node)) continue;
			p.Node.GetParent()?.RemoveChild(p.Node);
			p.Node.QueueFree();
		}
		Placed.Clear();
		occupiedCells.Clear();
		TownHall = null;

		foreach (var l in data["levels"].AsGodotDictionary())
			if (Levels.ContainsKey(l.Key.AsString())) Levels[l.Key.AsString()] = Mathf.Clamp(l.Value.AsInt32(), 1, Economy.Buildings[l.Key.AsString()].Levels.Length);
		foreach (var a in data["armory"].AsGodotDictionary())
			if (Armory.ContainsKey(a.Key.AsString())) Armory[a.Key.AsString()] = a.Value.AsInt32();
		ToolTier = Mathf.Clamp(data["tool_tier"].AsInt32(), 0, Economy.Tools.Length - 1);
		ArmorTier = Mathf.Clamp(data["armor_tier"].AsInt32(), 0, Economy.Armor.Length - 1);
		string hero = data["hero_weapon"].AsString();
		HeroWeapon = Economy.Weapons.ContainsKey(hero) ? hero : Economy.StartHeroWeapon;
		SetWorkerJob(data["worker_job"].AsString());
		Hungry = data["hungry"].AsBool();

		foreach (Variant entry in data["buildings"].AsGodotArray())
		{
			var b = entry.AsGodotDictionary();
			string id = b["id"].AsString();
			if (Economy.Buildings.ContainsKey(id)) SpawnBuilding(id, ReadCell(b["cell"]), b["hp"].AsInt32());
		}
		ApplyHeroStats();
		EmitSignal(SignalName.StockChanged);
		EmitSignal(SignalName.ProgressChanged);
	}

	private static Vector2I ReadCell(Variant cell)
	{
		if (cell.VariantType == Variant.Type.String) return GD.StrToVar("Vector2i" + cell.AsString()).AsVector2I();
		var xy = cell.AsGodotArray();
		return new Vector2I(xy[0].AsInt32(), xy[1].AsInt32());
	}
}
