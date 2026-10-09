using Godot;
using System.Collections.Generic;

public partial class GameHud : CanvasLayer
{
	[Export] public HBoxContainer StockBar;
	[Export] public Label ObjectiveLabel;
	[Export] public Label MessageLabel;
	[Export] public Control BossPanel;
	[Export] public ProgressBar BossBar;
	[Export] public Control EndPanel;
	[Export] public Label EndTitle;
	[Export] public Label EndReason;

	private static readonly Color FullColor = new(1f, 0.6f, 0.5f);

	private readonly Dictionary<string, Label> stockLabels = new();
	private double messageTimer;

	public override void _Ready()
	{
		GameManager game = GameManager.Instance;
		game.StockChanged += UpdateStock;
		game.ProgressChanged += UpdateObjective;
		game.ProgressChanged += UpdateStock;
		game.Message += ShowMessage;
		game.GameEnded += ShowEnd;
		BuildStock();
		UpdateStock();
		UpdateObjective();
	}

	public override void _ExitTree()
	{
		GameManager game = GameManager.Instance;
		game.StockChanged -= UpdateStock;
		game.ProgressChanged -= UpdateObjective;
		game.ProgressChanged -= UpdateStock;
		game.Message -= ShowMessage;
		game.GameEnded -= ShowEnd;
	}

	public override void _Process(double delta)
	{
		messageTimer -= delta;
		MessageLabel.Visible = messageTimer > 0;

		Node2D boss = GameManager.Instance.Boss;
		BossPanel.Visible = IsInstanceValid(boss);
		if (BossPanel.Visible)
		{
			BossBar.MaxValue = (int)boss.Get("max_hp");
			BossBar.Value = (int)boss.Get("hp");
		}
	}

	private void BuildStock()
	{
		foreach (string kind in Economy.Resources)
		{
			HBoxContainer item = new() { TooltipText = Economy.ResourceTitles[kind], MouseFilter = Control.MouseFilterEnum.Pass };
			item.AddThemeConstantOverride("separation", 6);
			item.AddChild(new TextureRect
			{
				Texture = GD.Load<Texture2D>($"res://UI/Icons/{kind}.png"),
				CustomMinimumSize = new Vector2(24, 24),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			});
			Label label = new() { ThemeTypeVariation = "HudLabel" };
			item.AddChild(label);
			StockBar.AddChild(item);
			stockLabels[kind] = label;
		}
	}

	private void UpdateStock()
	{
		GameManager game = GameManager.Instance;
		foreach (var (kind, label) in stockLabels)
		{
			int amount = game.GetStock(kind);
			bool capped = kind != Economy.Coins;
			label.Text = capped ? $"{amount}/{game.Capacity(kind)}" : $"{amount}";
			label.Modulate = capped && amount >= game.Capacity(kind) ? FullColor : Colors.White;
		}
	}

	private void UpdateObjective()
	{
		ObjectiveLabel.Text = GameManager.Instance.Objective();
	}

	private void ShowMessage(string text)
	{
		MessageLabel.Text = text;
		messageTimer = 2.5;
	}

	private void ShowEnd(bool victory, string reason)
	{
		EndTitle.Text = victory ? "Победа!" : "Поражение";
		EndReason.Text = reason;
		EndPanel.Visible = true;
		ObjectiveLabel.Text = "";
	}

	public void _on_restart_pressed() => GameManager.Instance.Restart();

	public void _on_menu_pressed() => GameManager.Instance.ToMenu();
}
