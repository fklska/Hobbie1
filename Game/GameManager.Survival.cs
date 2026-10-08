using Godot;
using System.Linq;

public partial class GameManager
{
	public const string RunsDir = "user://Runs/";
	public const int RunVersion = 1;

	public Survival Survival;
	public bool Restoring;

	public Node2D MainBase => TownHall;

	static GameManager()
	{
		KindTitles.TryAdd("coins", "Монеты");
		foreach (var (id, weapon) in Survival.FallbackWeapons) Costs.TryAdd(id, weapon.Cost);
	}

	private void StartSurvival()
	{
		Stock["coins"] = 0;
		Survival = new Survival { Name = "Survival" };
		GetTree().Root.AddChild(Survival);
	}

	public void OnHeroDied() => Survival?.OnHeroDied();

	public string SurvivalObjective() =>
		$"Продержитесь {Survival.WinDay} дней: по ночам идут враги, каждую {Survival.GiantEvery}-ю ночь — каменный гигант";

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest) SaveRun();
	}

	public static Godot.Collections.Array Vec(Vector2 v) => new() { v.X, v.Y };

	public static Vector2 ToVector(Variant value)
	{
		var xy = value.AsGodotArray();
		return new Vector2(xy[0].AsSingle(), xy[1].AsSingle());
	}

	public static string RunPath(string scenePath) => RunsDir + scenePath.GetFile().GetBaseName() + ".json";

	public bool HasRun(string scenePath) => ReadRun(scenePath) != null;

	public int RunDay(string scenePath)
	{
		var run = ReadRun(scenePath);
		return run == null ? 0 : run["survival"].AsGodotDictionary()["day"].AsInt32();
	}

	private static Godot.Collections.Dictionary ReadRun(string scenePath)
	{
		if (!FileAccess.FileExists(RunPath(scenePath))) return null;
		Variant data = Json.ParseString(FileAccess.GetFileAsString(RunPath(scenePath)));
		if (data.VariantType != Variant.Type.Dictionary) return null;
		var run = data.AsGodotDictionary();
		return run.ContainsKey("version") && run["version"].AsInt32() == RunVersion ? run : null;
	}

	public void ContinueRun(string scenePath)
	{
		var data = ReadRun(scenePath);
		if (data == null)
		{
			Notify("Сохранение партии не найдено");
			return;
		}
		Restoring = true;
		StartWorld(scenePath, ToVector(data["spawn"]));
		Restoring = false;
		ApplyRun(data);
	}

	public void SaveRun()
	{
		if (Ended || worldScenePath == null || !IsInstanceValid(World) || !IsInstanceValid(Player) || !IsInstanceValid(Survival)) return;
		DirAccess.MakeDirRecursiveAbsolute(RunsDir);
		using FileAccess file = FileAccess.Open(RunPath(worldScenePath), FileAccess.ModeFlags.Write);
		file?.StoreString(Json.Stringify(CaptureRun(), "\t"));
	}

	public void DeleteRun()
	{
		if (worldScenePath != null && FileAccess.FileExists(RunPath(worldScenePath))) DirAccess.RemoveAbsolute(RunPath(worldScenePath));
	}

	private Godot.Collections.Dictionary CaptureRun()
	{
		var stock = new Godot.Collections.Dictionary();
		foreach (var (kind, amount) in Stock) stock[kind] = amount;

		var buildings = new Godot.Collections.Array();
		foreach (var (id, node) in new[] { ("TownHall", TownHall), ("Blacksmith", Blacksmith) })
		{
			if (!IsInstanceValid(node)) continue;
			buildings.Add(new Godot.Collections.Dictionary
			{
				["id"] = id,
				["cell"] = Vec((node.Position / GenerationSettings.TILE_SIZE).Round()),
				["hp"] = node.Get("hp"),
			});
		}

		var workers = new Godot.Collections.Array();
		foreach (Node2D worker in Workers.Where(IsInstanceValid))
		{
			workers.Add(new Godot.Collections.Dictionary
			{
				["pos"] = Vec(worker.GlobalPosition),
				["hp"] = worker.Get("hp"),
				["weapon"] = worker.Get("weapon_id"),
			});
		}

		var data = new Godot.Collections.Dictionary
		{
			["version"] = RunVersion,
			["spawn"] = Vec(spawnPoint),
			["stock"] = stock,
			["buildings"] = buildings,
			["workers"] = workers,
			["worker_job"] = WorkerJob,
			["sword"] = SwordForged,
			["hero"] = new Godot.Collections.Dictionary
			{
				["pos"] = Vec(Player.IsDead ? Survival.BaseCenter() : Player.GlobalPosition),
				["hp"] = Player.IsDead ? Player.MaxHealth : Player.Health,
			},
			["survival"] = Survival.Capture(),
		};
		if (HasMethod("SaveEconomy")) data["economy"] = Call("SaveEconomy");
		return data;
	}

	private void ApplyRun(Godot.Collections.Dictionary data)
	{
		foreach (var (kind, amount) in data["stock"].AsGodotDictionary()) Stock[kind.AsString()] = amount.AsInt32();

		if (data.ContainsKey("economy") && HasMethod("LoadEconomy")) Call("LoadEconomy", data["economy"]);
		else
		{
			foreach (Variant entry in data["buildings"].AsGodotArray())
			{
				var building = entry.AsGodotDictionary();
				RestoreBuilding(building["id"].AsString(), (Vector2I)ToVector(building["cell"]), building["hp"].AsInt32());
			}
		}

		WorkerJob = data["worker_job"].AsString();
		SwordForged = data["sword"].AsBool();
		foreach (Variant entry in data["workers"].AsGodotArray())
		{
			var worker = entry.AsGodotDictionary();
			RestoreWorker(ToVector(worker["pos"]), worker["hp"].AsInt32(), worker["weapon"].AsString());
		}

		var hero = data["hero"].AsGodotDictionary();
		Player.GlobalPosition = ToVector(hero["pos"]);
		Player.Health = hero["hp"].AsSingle();
		Player.Heal(0);

		Survival.Restore(data["survival"].AsGodotDictionary());
		Survival.LoadAround(Player.GlobalPosition);
		EmitSignal(SignalName.StockChanged);
		EmitSignal(SignalName.ProgressChanged);
		Notify($"Партия продолжается: день {Survival.Day}");
	}

	private void RestoreBuilding(string id, Vector2I cell, int hp)
	{
		if (!Buildings.TryGetValue(id, out BuildingInfo info) || HasBuilding(id)) return;
		Node2D building = GD.Load<PackedScene>(info.ScenePath).Instantiate<Node2D>();
		building.Position = cell * GenerationSettings.TILE_SIZE;
		EntitiesRoot.AddChild(building);
		building.Set("hp", hp);
		building.Call("heal", 0);
		for (int x = 0; x < info.Footprint.X; x++)
			for (int y = 0; y < info.Footprint.Y; y++)
				occupiedCells.Add(cell + new Vector2I(x, y));
		if (id == "TownHall") TownHall = building;
		else Blacksmith = building;
		building.Connect("destroyed", Callable.From(() => OnBuildingDestroyed(id)));
	}

	private void RestoreWorker(Vector2 position, int hp, string weapon)
	{
		Node2D worker = GD.Load<PackedScene>("res://AI/Village/worker.tscn").Instantiate<Node2D>();
		worker.Position = position;
		EntitiesRoot.AddChild(worker);
		Workers.Add(worker);
		worker.TreeExiting += () => OnWorkerGone(worker);
		worker.Set("hp", hp);
		var stats = weapon == "" ? null : WeaponStats(weapon);
		if (stats?.Count > 0) worker.Call("equip", weapon, stats);
	}

	public Godot.Collections.Dictionary WeaponStats(string id)
	{
		if (HasMethod("GetWeaponStats")) return Call("GetWeaponStats", id).AsGodotDictionary();
		if (!Survival.FallbackWeapons.TryGetValue(id, out Survival.WeaponInfo weapon)) return new Godot.Collections.Dictionary();
		return new Godot.Collections.Dictionary
		{
			["id"] = id,
			["title"] = weapon.Title,
			["kind"] = weapon.Ranged ? "ranged" : "melee",
			["damage"] = weapon.Damage,
			["range"] = weapon.Range,
			["cooldown"] = weapon.Cooldown,
			["projectile_speed"] = weapon.ProjectileSpeed,
			["icon"] = weapon.Icon,
		};
	}

	public int ArmedWorkers => Workers.Count(w => IsInstanceValid(w) && w.Get("weapon_id").AsString() != "");

	public bool ArmWorker(Node2D worker, string weaponId)
	{
		if (Ended || !IsInstanceValid(worker)) return false;
		string current = worker.Get("weapon_id").AsString();
		if (current == weaponId) return true;
		var stats = weaponId == "" ? new Godot.Collections.Dictionary() : WeaponStats(weaponId);
		if (weaponId != "" && (stats.Count == 0 || !TakeArmoryWeapon(weaponId))) return false;
		if (current != "" && HasMethod("ReturnWeapon")) Call("ReturnWeapon", current);
		worker.Call("equip", weaponId, stats);
		EmitSignal(SignalName.ProgressChanged);
		return true;
	}

	public void ArmNextWorker(string weaponId)
	{
		Node2D worker = Workers.FirstOrDefault(w => IsInstanceValid(w) && w.Get("weapon_id").AsString() == "");
		if (worker == null)
		{
			Notify(Workers.Count == 0 ? "Сначала наймите жителя" : "Все жители уже вооружены");
			return;
		}
		if (ArmWorker(worker, weaponId)) Notify($"Житель вооружён: {WeaponStats(weaponId)["title"]}");
	}

	private bool TakeArmoryWeapon(string id)
	{
		if (HasMethod("TakeWeapon")) return Call("TakeWeapon", id).AsBool();
		return Costs.ContainsKey(id) && TrySpend(id);
	}
}
