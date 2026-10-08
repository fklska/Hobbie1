using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
	[Signal] public delegate void StockChangedEventHandler();
	[Signal] public delegate void ProgressChangedEventHandler();
	[Signal] public delegate void MessageEventHandler(string text);
	[Signal] public delegate void GameEndedEventHandler(bool victory, string reason);

	public record BuildingInfo(string Title, string ScenePath, Vector2I Footprint, string GhostPath, float GhostScale);

	public static GameManager Instance { get; private set; }

	public static readonly string[] Kinds = { "wood", "stone", "iron", "gold" };

	public static readonly Dictionary<string, string> KindTitles = new()
	{
		["wood"] = "Дерево",
		["stone"] = "Камень",
		["iron"] = "Железо",
		["gold"] = "Золото",
	};

	public static readonly Dictionary<string, BuildingInfo> Buildings = new()
	{
		["TownHall"] = new("Ратуша", "res://BuildSystem/buildings/TownHall.tscn", new Vector2I(1, 1), "res://kenney_medieval-rts/Default size/Structure/medievalStructure_19.png", 1f),
		["Blacksmith"] = new("Кузница", "res://BuildSystem/buildings/black_smith.tscn", new Vector2I(3, 2), "res://BuildSystem/assets/370PEG2-0.png", 0.5f),
	};

	public static readonly Dictionary<string, Dictionary<string, int>> Costs = new()
	{
		["TownHall"] = new() { ["wood"] = 10, ["stone"] = 5 },
		["Blacksmith"] = new() { ["wood"] = 20, ["stone"] = 10 },
		["Worker"] = new() { ["wood"] = 8 },
		["Sword"] = new() { ["iron"] = 5, ["wood"] = 5 },
	};

	public const int MaxWorkers = 5;

	public Dictionary<string, int> Stock = new();
	public WorldScene World;
	public PlayerMainCharacter Player;
	public Node2D TownHall;
	public Node2D Blacksmith;
	public Node2D Boss;
	public List<Node2D> Workers = new();
	public string WorkerJob = "wood";
	public bool SwordForged;
	public bool Ended;

	private readonly HashSet<Vector2I> occupiedCells = new();
	private CanvasLayer hud;
	private string worldScenePath;
	private Vector2 spawnPoint;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		ResetState();
	}

	public void ResetState()
	{
		foreach (string kind in Kinds) Stock[kind] = 0;
		World = null;
		Player = null;
		TownHall = null;
		Blacksmith = null;
		Boss = null;
		Workers.Clear();
		occupiedCells.Clear();
		WorkerJob = "wood";
		SwordForged = false;
		Ended = false;
		Grid.StopPlacement();
	}

	public void StartWorld(string scenePath, Vector2 spawn)
	{
		Cleanup();
		ResetState();
		worldScenePath = scenePath;
		spawnPoint = spawn;
		GetTree().Paused = false;

		Window root = GetTree().Root;
		World = GD.Load<PackedScene>(scenePath).Instantiate<WorldScene>();
		root.AddChild(World);

		Player = GD.Load<PackedScene>("res://Player/Player.tscn").Instantiate<PlayerMainCharacter>();
		Player.Position = spawn;
		root.AddChild(Player);

		hud = GD.Load<PackedScene>("res://Game/game_hud.tscn").Instantiate<CanvasLayer>();
		root.AddChild(hud);
		StartSurvival();

		root.GetNodeOrNull<Control>("World/UI/Menu")?.Hide();
		EmitSignal(SignalName.StockChanged);
		EmitSignal(SignalName.ProgressChanged);
	}

	public void Restart()
	{
		if (worldScenePath != null) StartWorld(worldScenePath, spawnPoint);
	}

	public void ToMenu()
	{
		Cleanup();
		ResetState();
		GetTree().Paused = false;
		GetTree().Root.GetNodeOrNull<Control>("World/UI/Menu")?.Show();
	}

	private void Cleanup()
	{
		SaveRun();
		foreach (Node node in new Node[] { Survival, hud, Player, World })
		{
			if (!IsInstanceValid(node)) continue;
			node.GetParent()?.RemoveChild(node);
			node.QueueFree();
		}
		hud = null;
		Survival = null;
	}

	public Node2D EntitiesRoot => World?.GetNode<Node2D>("Enviroment");

	public static string KindOf(ResorseType type) => type switch
	{
		ResorseType.None => "",
		ResorseType.Stone => "stone",
		ResorseType.Iron => "iron",
		ResorseType.Gold => "gold",
		_ => "wood",
	};

	public static int YieldOf(ResorseType type) => type switch
	{
		ResorseType.GiantWood or ResorseType.GiantSnowWood => 3,
		ResorseType.MediumWood or ResorseType.MediumSnowWood or ResorseType.Stone => 2,
		ResorseType.None => 0,
		_ => 1,
	};

	public string GetKindOf(ResorseType type) => KindOf(type);

	public int GetYieldOf(ResorseType type) => YieldOf(type);

	public int GetStock(string kind) => Stock.GetValueOrDefault(kind);

	public void AddResource(string kind, int amount)
	{
		if (!Stock.ContainsKey(kind) || amount <= 0) return;
		Stock[kind] += amount;
		EmitSignal(SignalName.StockChanged);
	}

	public void AddHarvest(ResorseType type) => AddResource(KindOf(type), YieldOf(type));

	public bool CanAfford(string costId) => Costs[costId].All(c => Stock[c.Key] >= c.Value);

	public bool TrySpend(string costId)
	{
		if (!CanAfford(costId))
		{
			Notify($"Не хватает ресурсов: {CostText(costId)}");
			return false;
		}
		foreach (var c in Costs[costId]) Stock[c.Key] -= c.Value;
		EmitSignal(SignalName.StockChanged);
		return true;
	}

	public static string CostText(string costId) =>
		string.Join(", ", Costs[costId].Select(c => $"{KindTitles[c.Key].ToLower()} {c.Value}"));

	public void Notify(string text) => EmitSignal(SignalName.Message, text);

	public bool HasBuilding(string id) => id switch
	{
		"TownHall" => IsInstanceValid(TownHall),
		"Blacksmith" => IsInstanceValid(Blacksmith),
		_ => false,
	};

	public bool CanPlace(string id, Vector2I cell)
	{
		if (World == null) return false;
		Grid grid = GetNode<Grid>("/root/BuildMode");
		Vector2I footprint = Buildings[id].Footprint;
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
		if (Ended) return false;
		if (HasBuilding(id))
		{
			Notify($"{Buildings[id].Title} уже построена");
			return true;
		}
		if (id == "Blacksmith" && !HasBuilding("TownHall"))
		{
			Notify("Сначала постройте ратушу");
			return true;
		}
		if (!CanPlace(id, cell))
		{
			Notify("Здесь строить нельзя");
			return false;
		}
		if (!TrySpend(id)) return true;

		BuildingInfo info = Buildings[id];
		Node2D building = GD.Load<PackedScene>(info.ScenePath).Instantiate<Node2D>();
		building.Position = cell * GenerationSettings.TILE_SIZE;
		EntitiesRoot.AddChild(building);

		for (int x = 0; x < info.Footprint.X; x++)
			for (int y = 0; y < info.Footprint.Y; y++)
				occupiedCells.Add(cell + new Vector2I(x, y));

		if (id == "TownHall") TownHall = building;
		else Blacksmith = building;

		building.Connect("destroyed", Callable.From(() => OnBuildingDestroyed(id)));
		Notify($"{info.Title} построена");
		EmitSignal(SignalName.ProgressChanged);
		return true;
	}

	private void OnBuildingDestroyed(string id)
	{
		if (id == "TownHall") EndGame(false, "Ратуша разрушена");
		else
		{
			Blacksmith = null;
			Notify("Кузница разрушена");
			EmitSignal(SignalName.ProgressChanged);
		}
	}

	public void HireWorker()
	{
		if (Ended) return;
		if (!HasBuilding("TownHall"))
		{
			Notify("Сначала постройте ратушу");
			return;
		}
		if (Workers.Count >= MaxWorkers)
		{
			Notify($"Больше {MaxWorkers} жителей не прокормить");
			return;
		}
		if (!TrySpend("Worker")) return;

		Node2D worker = GD.Load<PackedScene>("res://AI/Village/worker.tscn").Instantiate<Node2D>();
		worker.Position = BuildingCenter(TownHall) + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 72f;
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
		if (Stock.ContainsKey(kind)) WorkerJob = kind;
	}

	public void ForgeSword()
	{
		if (Ended) return;
		if (SwordForged)
		{
			Notify("Меч уже выкован");
			return;
		}
		if (!HasBuilding("Blacksmith"))
		{
			Notify("Сначала постройте кузницу");
			return;
		}
		if (!TrySpend("Sword")) return;
		SwordForged = true;
		Notify("Меч выкован: урон героя утроен");
		EmitSignal(SignalName.ProgressChanged);
	}

	public static Vector2 BuildingCenter(Node2D building) => (Vector2)building.Call("get_center");

	public void Damage(Node target, int amount)
	{
		if (!IsInstanceValid(target)) return;
		if (target.HasMethod("take_damage")) target.Call("take_damage", amount);
		else if (target.HasMethod(PlayerMainCharacter.MethodName.TakeDamage)) target.Call(PlayerMainCharacter.MethodName.TakeDamage, amount);
	}

	public string Objective()
	{
		if (Ended) return "";
		if (!HasBuilding("TownHall")) return $"Добудьте ресурсы и постройте ратушу ({CostText("TownHall")})";
		if (Workers.Count == 0) return $"Наймите жителя во вкладке «Деревня» ({CostText("Worker")})";
		if (!HasBuilding("Blacksmith")) return $"Постройте кузницу ({CostText("Blacksmith")})";
		if (!SwordForged) return $"Выкуйте меч в кузнице ({CostText("Sword")})";
		return SurvivalObjective();
	}

	public void EndGame(bool victory, string reason)
	{
		if (Ended) return;
		Ended = true;
		Grid.StopPlacement();
		EmitSignal(SignalName.GameEnded, victory, reason);
		GetTree().Paused = true;
	}
}
