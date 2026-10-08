using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Survival : Node
{
	[Signal] public delegate void ChangedEventHandler();

	public record MobInfo(string Title, string ScenePath, int FromDay, int Cost);
	public record ArtifactInfo(string Title, string Effect);

	public const int WinDay = 30;
	public const int GiantEvery = 5;
	public const string GiantScene = "res://AI/BaseClasses/Enemy/stone_giant.tscn";
	public const string LootScene = "res://Game/Loot/loot_pickup.tscn";
	public const string HudScene = "res://Game/survival_hud.tscn";
	public const float SpawnDistance = 1300f;
	public const float GroupInterval = 8f;
	public const int MaxAlive = 45;
	public const float HeroRespawnTime = 15f;

	public static readonly Dictionary<string, MobInfo> Mobs = new()
	{
		["goblin"] = new("Гоблин", "res://AI/Enemies/goblin.tscn", 1, 1),
		["wolf"] = new("Волк", "res://AI/Enemies/wolf.tscn", 2, 1),
		["skeleton_archer"] = new("Скелет-лучник", "res://AI/Enemies/skeleton_archer.tscn", 4, 2),
		["orc"] = new("Орк-громила", "res://AI/Enemies/orc.tscn", 7, 4),
	};

	public static readonly Dictionary<string, ArtifactInfo> Artifacts = new()
	{
		["fang"] = new("Клык вожака", "урон героя +4"),
		["stone_heart"] = new("Каменное сердце", "здоровье героя +20"),
		["boots"] = new("Сапоги гонца", "скорость героя +8%"),
		["banner"] = new("Боевое знамя", "урон жителей +25%"),
		["totem"] = new("Тотем очага", "главное здание чинится на 1 в секунду"),
		["lucky_coin"] = new("Счастливая монета", "монет с врагов +50%"),
	};

	public int LastWaveDay;
	public readonly List<string> SpawnQueue = new();
	public readonly List<Node2D> AliveMobs = new();
	public readonly Dictionary<string, int> Found = new();
	public readonly Dictionary<Vector2I, int> Harvested = new();
	public Node2D Giant;
	public double RespawnTimer;
	public Node Cycle { get; private set; }

	private GameManager game;
	private Curve nightCurve;
	private float nightStart = 0.8f;
	private float nightEnd = 0.25f;
	private double groupTimer;
	private double regenBuffer;
	private Vector2 waveDirection = Vector2.Right;

	public int Day => Cycle.Get("day").AsInt32();
	public float TimeOfDay => Cycle.Get("time_of_day").AsSingle();
	public bool IsNight => Cycle.Get("is_night").AsBool();
	public float DayLengthSeconds => Cycle.Get("day_length_minutes").AsSingle() * 60f;
	public float SecondsUntilNight => Mathf.PosMod(nightStart - TimeOfDay, 1f) * DayLengthSeconds;
	public float SecondsUntilDawn => Mathf.PosMod(nightEnd - TimeOfDay, 1f) * DayLengthSeconds;
	public int EnemiesLeft => AliveMobs.Count + SpawnQueue.Count;
	public float VillagerDamageMultiplier => 1f + 0.25f * Count("banner");
	public float CoinMultiplier => 1f + 0.5f * Count("lucky_coin");

	public int Count(string artifact) => Found.GetValueOrDefault(artifact);

	public static bool IsGiantDay(int day) => day % GiantEvery == 0;

	public int DaysUntilGiant
	{
		get
		{
			int next = LastWaveDay >= Day ? Day + 1 : Day;
			while (!IsGiantDay(next)) next++;
			return next > WinDay ? -1 : next - Day;
		}
	}

	public override void _Ready()
	{
		game = GameManager.Instance;
		Cycle = game.Player.FindChild("DayNight", true, false);
		nightCurve = Cycle.Get("night_curve").As<Curve>();
		FindNightBounds();
		Cycle.Connect("night_started", Callable.From<int>(OnNightStarted));
		Cycle.Connect("day_started", Callable.From<int>(OnDayStarted));
		Cycle.Connect("new_day", Callable.From<int>(_ => EmitSignal(SignalName.Changed)));
		game.World.TileHarvested += OnTileHarvested;
		game.GameEnded += OnGameEnded;
		AddChild(GD.Load<PackedScene>(HudScene).Instantiate());
	}

	public override void _ExitTree()
	{
		game.GameEnded -= OnGameEnded;
	}

	public override void _Process(double delta)
	{
		if (game.Ended) return;
		groupTimer -= delta;
		if (SpawnQueue.Count > 0 && groupTimer <= 0 && AliveMobs.Count < MaxAlive)
		{
			SpawnGroup();
			groupTimer = GroupInterval;
		}
		if (RespawnTimer > 0)
		{
			RespawnTimer -= delta;
			if (RespawnTimer <= 0) RespawnHero();
		}
		RegenBase(delta);
	}

	private void FindNightBounds()
	{
		const int steps = 1440;
		for (int i = 0; i < steps; i++)
		{
			float t = (float)i / steps;
			bool before = NightAt(t);
			bool after = NightAt(t + 1f / steps);
			if (!before && after) nightStart = t;
			if (before && !after) nightEnd = t;
		}
	}

	public bool NightAt(float t) => nightCurve.SampleBaked(Mathf.PosMod(t, 1f)) >= 0.5f;

	public Vector2 BaseCenter() =>
		IsInstanceValid(game.MainBase) ? GameManager.BuildingCenter(game.MainBase) : game.Player.GlobalPosition;

	private void OnNightStarted(int day)
	{
		if (game.Ended || day <= LastWaveDay) return;
		LastWaveDay = day;
		SpawnQueue.AddRange(ComposeWave(day));
		groupTimer = 0;
		waveDirection = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
		bool giant = IsGiantDay(day);
		if (giant) PlaySound("giant_roar", SpawnGiant(day, SpawnPoint(BaseCenter(), waveDirection)).GlobalPosition);
		game.Notify(giant ? $"Ночь {day}: каменный гигант идёт к деревне!" : $"Ночь {day}: враги наступают");
		EmitSignal(SignalName.Changed);
	}

	private void OnDayStarted(int day)
	{
		if (game.Ended) return;
		if (day > WinDay)
		{
			game.EndGame(true, $"Деревня продержалась {WinDay} дней");
			return;
		}
		game.Notify($"Рассвет. День {day} из {WinDay}");
		game.SaveRun();
		EmitSignal(SignalName.Changed);
	}

	private void OnGameEnded(bool victory, string reason) => game.DeleteRun();

	private void OnTileHarvested(Vector2I cell, ResorseType type) => Harvested[cell] = (int)type;

	public static List<string> ComposeWave(int day)
	{
		int budget = 2 + (int)(day * 1.6f);
		var options = Mobs.Where(m => m.Value.FromDay <= day).ToList();
		var wave = new List<string>();
		while (true)
		{
			var affordable = options.Where(o => o.Value.Cost <= budget).ToList();
			if (affordable.Count == 0) break;
			var pick = affordable[GD.RandRange(0, affordable.Count - 1)];
			wave.Add(pick.Key);
			budget -= pick.Value.Cost;
		}
		return wave;
	}

	private void SpawnGroup()
	{
		int size = Mathf.Min(SpawnQueue.Count, 3 + LastWaveDay / 4);
		Vector2 center = BaseCenter();
		for (int i = 0; i < size; i++)
		{
			string id = SpawnQueue[0];
			SpawnQueue.RemoveAt(0);
			SpawnMob(id, SpawnPoint(center, waveDirection.Rotated((float)GD.RandRange(-0.5, 0.5))));
		}
		if (GD.Randf() < 0.3f) waveDirection = waveDirection.Rotated((float)GD.RandRange(-1.2, 1.2));
		EmitSignal(SignalName.Changed);
	}

	private Vector2 SpawnPoint(Vector2 center, Vector2 direction)
	{
		Vector2 point = center + direction * (SpawnDistance + (float)GD.RandRange(0.0, 200.0));
		Vector2 size = game.World.GeneratorData.mapSize * GenerationSettings.TILE_SIZE;
		return point.Clamp(Vector2.One * 64f, size - Vector2.One * 64f);
	}

	public Node2D SpawnMob(string id, Vector2 position)
	{
		Node2D mob = GD.Load<PackedScene>(Mobs[id].ScenePath).Instantiate<Node2D>();
		mob.Position = position;
		game.EntitiesRoot.AddChild(mob);
		Track(mob);
		mob.Connect("died", Callable.From<Node2D>(OnMobDied));
		return mob;
	}

	public Node2D SpawnGiant(int day, Vector2 position)
	{
		Node2D giant = GD.Load<PackedScene>(GiantScene).Instantiate<Node2D>();
		giant.Set("hp_per_health", 6 + 2 * (Mathf.Max(day / GiantEvery, 1) - 1));
		giant.Position = position;
		game.EntitiesRoot.AddChild(giant);
		Giant = giant;
		game.Boss = giant;
		Track(giant);
		giant.Connect("died", Callable.From(() => OnGiantDied(giant)));
		return giant;
	}

	private void Track(Node2D mob)
	{
		AliveMobs.Add(mob);
		mob.TreeExiting += () =>
		{
			AliveMobs.Remove(mob);
			if (IsInsideTree()) EmitSignal(SignalName.Changed);
		};
	}

	private void OnMobDied(Node2D mob)
	{
		DropLoot(mob.GlobalPosition, mob.Get("coins").AsVector2I(), mob.Get("artifact_chance").AsSingle());
	}

	private void OnGiantDied(Node2D giant)
	{
		DropLoot(giant.GlobalPosition, new Vector2I(30, 50), 1f);
		game.Notify("Каменный гигант повержен!");
		giant.QueueFree();
	}

	public void DropLoot(Vector2 at, Vector2I coinRange, float artifactChance)
	{
		int coins = Mathf.RoundToInt(GD.RandRange(coinRange.X, coinRange.Y) * CoinMultiplier);
		if (coins > 0)
		{
			SpawnPickup(at, coins, "");
			PlaySound("coin", at);
		}
		if (GD.Randf() < artifactChance)
		{
			string[] ids = Artifacts.Keys.ToArray();
			SpawnPickup(at + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 24f, 0, ids[GD.RandRange(0, ids.Length - 1)]);
		}
	}

	private void SpawnPickup(Vector2 at, int coins, string artifact)
	{
		Node2D pickup = GD.Load<PackedScene>(LootScene).Instantiate<Node2D>();
		pickup.Position = at;
		pickup.Set("coins", coins);
		pickup.Set("artifact", artifact);
		game.EntitiesRoot.CallDeferred(Node.MethodName.AddChild, pickup);
	}

	private void PlaySound(string id, Vector2 at) => GetNodeOrNull("/root/Sound")?.Call("PlayAt", id, at);

	public void CollectCoins(int amount) => game.AddResource("coins", amount);

	public void CollectArtifact(string id)
	{
		if (!Artifacts.TryGetValue(id, out ArtifactInfo info)) return;
		Found[id] = Count(id) + 1;
		ApplyArtifact(id, 1);
		PlaySound("pickup", game.Player.GlobalPosition);
		game.Notify($"Артефакт «{info.Title}»: {info.Effect}");
		EmitSignal(SignalName.Changed);
	}

	public void ApplyArtifact(string id, int count)
	{
		PlayerMainCharacter player = game.Player;
		switch (id)
		{
			case "stone_heart":
				game.ApplyHeroStats();
				break;
			case "boots":
				player.SPEED *= Mathf.Pow(1.08f, count);
				break;
		}
	}

	public void OnHeroDied()
	{
		RespawnTimer = HeroRespawnTime;
		game.Player.RemoveFromGroup("village");
		game.Notify($"Герой пал. Возрождение у главного здания через {HeroRespawnTime:0} с");
	}

	private void RespawnHero()
	{
		Vector2 at = BaseCenter() + new Vector2(0, 90);
		LoadAround(at);
		game.Player.Respawn(at);
		game.Player.AddToGroup("village");
	}

	public void LoadAround(Vector2 at)
	{
		WorldScene world = game.World;
		Vector2I chunk = GenerationUtils.PixelToChunkCoord(at);
		foreach (Vector2I c in GenerationUtils.ChunckAreaCoords(chunk, 4))
			_ = world.LoadChunk(c, world.MainTileMapPrefab, world.EnviromentLayer, world.Enviroment, world.Enviroment);
		world.lastPlayerCell = chunk;
	}

	private void RegenBase(double delta)
	{
		int totems = Count("totem");
		if (totems == 0 || !IsInstanceValid(game.MainBase)) return;
		regenBuffer += totems * delta;
		if (regenBuffer < 1) return;
		int amount = (int)regenBuffer;
		regenBuffer -= amount;
		game.MainBase.Call("heal", amount);
	}

	public Godot.Collections.Dictionary Capture()
	{
		var mobs = new Godot.Collections.Array();
		foreach (Node2D mob in AliveMobs)
		{
			if (!IsInstanceValid(mob) || mob.Get("hp").AsInt32() <= 0 || mob.Get("dead").AsBool()) continue;
			mobs.Add(new Godot.Collections.Dictionary
			{
				["id"] = mob == Giant ? "giant" : mob.Get("mob_id").AsString(),
				["pos"] = GameManager.Vec(mob.GlobalPosition),
				["hp"] = mob.Get("hp"),
			});
		}
		var queue = new Godot.Collections.Array();
		foreach (string id in SpawnQueue) queue.Add(id);
		var found = new Godot.Collections.Dictionary();
		foreach (var (id, count) in Found) found[id] = count;
		var harvested = new Godot.Collections.Array();
		foreach (Vector2I cell in Harvested.Keys) harvested.Add(new Godot.Collections.Array { cell.X, cell.Y });
		return new Godot.Collections.Dictionary
		{
			["day"] = Day,
			["time"] = TimeOfDay,
			["last_wave"] = LastWaveDay,
			["queue"] = queue,
			["mobs"] = mobs,
			["artifacts"] = found,
			["harvested"] = harvested,
		};
	}

	public void Restore(Godot.Collections.Dictionary data)
	{
		float time = data["time"].AsSingle();
		Cycle.Set("day", data["day"].AsInt32());
		Cycle.Call("set_hour", time * 24f);
		Cycle.Set("is_night", NightAt(time));
		LastWaveDay = data["last_wave"].AsInt32();
		foreach (Variant id in data["queue"].AsGodotArray()) SpawnQueue.Add(id.AsString());
		foreach (Variant cell in data["harvested"].AsGodotArray())
		{
			var xy = cell.AsGodotArray();
			game.World.HarvestTile(new Vector2I(xy[0].AsInt32(), xy[1].AsInt32()));
		}
		foreach (var (id, count) in data["artifacts"].AsGodotDictionary())
		{
			if (!Artifacts.ContainsKey(id.AsString())) continue;
			Found[id.AsString()] = count.AsInt32();
			ApplyArtifact(id.AsString(), count.AsInt32());
		}
		foreach (Variant entry in data["mobs"].AsGodotArray())
		{
			var mob = entry.AsGodotDictionary();
			string id = mob["id"].AsString();
			Vector2 position = GameManager.ToVector(mob["pos"]);
			Node2D node = id == "giant" ? SpawnGiant(LastWaveDay, position) : Mobs.ContainsKey(id) ? SpawnMob(id, position) : null;
			node?.Set("hp", mob["hp"].AsInt32());
		}
		EmitSignal(SignalName.Changed);
	}
}
