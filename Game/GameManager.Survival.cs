using Godot;
using System.Linq;

public partial class GameManager
{
	public const string RunsDir = "user://Runs/";
	public const int RunVersion = 2;

	public Survival Survival;
	public bool Restoring;

	private void StartSurvival()
	{
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
		if (!IsInstanceValid(World)) return;
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
			["economy"] = SaveEconomy(),
			["workers"] = workers,
			["hero"] = new Godot.Collections.Dictionary
			{
				["pos"] = Vec(Player.IsDead ? Survival.BaseCenter() : Player.GlobalPosition),
				["hp"] = Player.IsDead ? Player.MaxHealth : Player.Health,
				["class"] = HeroClass,
				["skills"] = SaveHero()["skills"],
			},
			["survival"] = Survival.Capture(),
		};
		return data;
	}

	private void ApplyRun(Godot.Collections.Dictionary data)
	{
		foreach (var (kind, amount) in data["stock"].AsGodotDictionary()) Stock[kind.AsString()] = amount.AsInt32();

		LoadEconomy(data["economy"].AsGodotDictionary());
		foreach (Variant entry in data["workers"].AsGodotArray())
		{
			var worker = entry.AsGodotDictionary();
			RestoreWorker(World.Walkable(ToVector(worker["pos"])), worker["hp"].AsInt32(), worker["weapon"].AsString());
		}

		var hero = data["hero"].AsGodotDictionary();
		Player.GlobalPosition = World.Walkable(ToVector(hero["pos"]));

		Survival.Restore(data["survival"].AsGodotDictionary());
		LoadHero(hero);
		Player.Health = Mathf.Min(hero["hp"].AsSingle(), Player.MaxHealth);
		Player.Heal(0);
		Survival.LoadAround(Player.GlobalPosition);
		EmitSignal(SignalName.StockChanged);
		EmitSignal(SignalName.ProgressChanged);
		Notify($"Партия продолжается: день {Survival.Day}");
	}

	private void RestoreWorker(Vector2 position, int hp, string weapon)
	{
		Node2D worker = GD.Load<PackedScene>("res://AI/Village/worker.tscn").Instantiate<Node2D>();
		worker.Position = position;
		EntitiesRoot.AddChild(worker);
		Workers.Add(worker);
		worker.TreeExiting += () => OnWorkerGone(worker);
		worker.Set("hp", hp);
		if (Economy.Weapons.ContainsKey(weapon)) worker.Call("equip", weapon, GetWeaponStats(weapon));
	}

	public int ArmedWorkers => Workers.Count(w => IsInstanceValid(w) && w.Get("weapon_id").AsString() != "");

	public bool ArmWorker(Node2D worker, string weaponId)
	{
		if (Ended || !IsInstanceValid(worker)) return false;
		string current = worker.Get("weapon_id").AsString();
		if (current == weaponId) return true;
		if (weaponId != "" && (!Economy.Weapons.ContainsKey(weaponId) || !TakeWeapon(weaponId))) return false;
		var stats = weaponId == "" ? new Godot.Collections.Dictionary() : GetWeaponStats(weaponId);
		if (current != "") ReturnWeapon(current);
		worker.Call("equip", weaponId, stats);
		EmitSignal(SignalName.ProgressChanged);
		return true;
	}

	public void ArmNextWorker(string weaponId)
	{
		if (ArmedWorkers >= MilitiaCap)
		{
			Notify("Ополчение заполнено: улучшите центр или постройте казарму");
			return;
		}
		Node2D worker = Workers.FirstOrDefault(w => IsInstanceValid(w) && w.Get("weapon_id").AsString() == "");
		if (worker == null)
		{
			Notify(Workers.Count == 0 ? "Сначала наймите жителя" : "Все жители уже вооружены");
			return;
		}
		if (ArmWorker(worker, weaponId)) Notify($"Житель вооружён: {Economy.Weapons[weaponId].Title.ToLower()}");
		else Notify("В арсенале нет такого оружия");
	}
}
