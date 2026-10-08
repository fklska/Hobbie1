using Godot;
using System.Collections.Generic;

public partial class GameManager : Node
{
	[Signal] public delegate void StockChangedEventHandler();
	[Signal] public delegate void ProgressChangedEventHandler();
	[Signal] public delegate void MessageEventHandler(string text);
	[Signal] public delegate void GameEndedEventHandler(bool victory, string reason);

	public static GameManager Instance { get; private set; }

	public Dictionary<string, int> Stock = new();
	public WorldScene World;
	public PlayerMainCharacter Player;
	public Node2D TownHall;
	public Node2D Boss;
	public List<Node2D> Workers = new();
	public string WorkerJob = "wood";
	public bool Ended;

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
		World = null;
		Player = null;
		TownHall = null;
		Boss = null;
		Workers.Clear();
		WorkerJob = "wood";
		Ended = false;
		ResetEconomy();
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
		StartEconomy(spawn);
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

	public void Notify(string text) => EmitSignal(SignalName.Message, text);

	public static Vector2 BuildingCenter(Node2D building) => (Vector2)building.Call("get_center");

	public void Damage(Node target, int amount)
	{
		if (!IsInstanceValid(target)) return;
		if (target is Node2D body) SoundManager.Instance.PlayAt(target == Player ? "hurt" : "hit", body.GlobalPosition);
		if (target.HasMethod("take_damage")) target.Call("take_damage", amount);
		else if (target.HasMethod(PlayerMainCharacter.MethodName.TakeDamage)) target.Call(PlayerMainCharacter.MethodName.TakeDamage, amount);
	}

	public string Objective()
	{
		if (Ended) return "";
		if (IsInstanceValid(Boss)) return "Победите каменного гиганта, пока он не разрушил поселение";
		string hint = EconomyObjective();
		return hint != "" ? hint : SurvivalObjective();
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
