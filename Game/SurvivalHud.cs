using Godot;
using System.Linq;

public partial class SurvivalHud : CanvasLayer
{
	[Export] public Label DayLabel;
	[Export] public Label ClockLabel;
	[Export] public Label WaveLabel;
	[Export] public Label GiantLabel;
	[Export] public Label CoinsLabel;
	[Export] public Label ArtifactsLabel;
	[Export] public Label RespawnLabel;

	private Survival survival;

	public override void _Ready()
	{
		survival = GetParent<Survival>();
		survival.Changed += Refresh;
		GameManager.Instance.StockChanged += Refresh;
		Refresh();
	}

	public override void _ExitTree()
	{
		survival.Changed -= Refresh;
		GameManager.Instance.StockChanged -= Refresh;
	}

	public override void _Process(double delta)
	{
		int minutes = (int)(survival.TimeOfDay * 24 * 60);
		string clock = $"{minutes / 60:00}:{minutes % 60:00}";
		ClockLabel.Text = survival.IsNight
			? $"{clock}  ночь, до рассвета {Duration(survival.SecondsUntilDawn)}"
			: $"{clock}  до ночи {Duration(survival.SecondsUntilNight)}";

		RespawnLabel.Visible = survival.RespawnTimer > 0;
		if (RespawnLabel.Visible) RespawnLabel.Text = $"Герой возродится через {Mathf.CeilToInt(survival.RespawnTimer)} с";
	}

	private void Refresh()
	{
		if (!IsInsideTree()) return;
		DayLabel.Text = $"День {Mathf.Min(survival.Day, Survival.WinDay)} из {Survival.WinDay}";

		int left = survival.EnemiesLeft;
		WaveLabel.Visible = left > 0;
		WaveLabel.Text = $"Врагов: {left}";

		int untilGiant = survival.DaysUntilGiant;
		GiantLabel.Visible = IsInstanceValid(survival.Giant) || untilGiant >= 0;
		GiantLabel.Text = IsInstanceValid(survival.Giant) ? "Каменный гигант в деревне!"
			: untilGiant == 0 ? "Гигант придёт этой ночью"
			: $"Гигант придёт через {untilGiant} {Days(untilGiant)}";

		CoinsLabel.Text = $"Монеты: {GameManager.Instance.GetStock("coins")}";

		int artifacts = survival.Found.Values.Sum();
		ArtifactsLabel.Visible = artifacts > 0;
		ArtifactsLabel.Text = $"Артефакты: {artifacts}";
		ArtifactsLabel.TooltipText = string.Join("\n", survival.Found.Select(a =>
			$"{Survival.Artifacts[a.Key].Title} ×{a.Value}: {Survival.Artifacts[a.Key].Effect}"));
	}

	private static string Duration(float seconds)
	{
		int total = Mathf.CeilToInt(seconds);
		return $"{total / 60}:{total % 60:00}";
	}

	private static string Days(int n) =>
		(n % 10 == 1 && n % 100 != 11) ? "день"
		: (n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20)) ? "дня"
		: "дней";
}
