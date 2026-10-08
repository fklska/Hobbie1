using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameHud : CanvasLayer
{
	[Export] public Label StockLabel;
	[Export] public Label ObjectiveLabel;
	[Export] public Label MessageLabel;
	[Export] public Control BossPanel;
	[Export] public ProgressBar BossBar;
	[Export] public Control EndPanel;
	[Export] public Label EndTitle;
	[Export] public Label EndReason;

	private double messageTimer;

	public override void _Ready()
	{
		GameManager game = GameManager.Instance;
		game.StockChanged += UpdateStock;
		game.ProgressChanged += UpdateObjective;
		game.Message += ShowMessage;
		game.GameEnded += ShowEnd;
		UpdateStock();
		UpdateObjective();
	}

	public override void _ExitTree()
	{
		GameManager game = GameManager.Instance;
		game.StockChanged -= UpdateStock;
		game.ProgressChanged -= UpdateObjective;
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

	private void UpdateStock()
	{
		Dictionary<string, int> stock = GameManager.Instance.Stock;
		StockLabel.Text = string.Join("    ", GameManager.Kinds.Select(k => $"{GameManager.KindTitles[k]}: {stock[k]}"));
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
