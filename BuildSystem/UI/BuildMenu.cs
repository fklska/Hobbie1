using Godot;

public partial class BuildMenu : TabContainer
{
	[Export] public Button TownHallButton;
	[Export] public Button BlacksmithButton;
	[Export] public Label WorkersLabel;
	[Export] public Button HireButton;
	[Export] public OptionButton JobOption;
	[Export] public Button SwordButton;
	[Export] public Button BossButton;

	public override void _Ready()
	{
		SetTabTitle(0, "Постройки");
		SetTabTitle(1, "Деревня");
		TownHallButton.Text = $"Ратуша\n{GameManager.CostText("TownHall")}";
		BlacksmithButton.Text = $"Кузница\n{GameManager.CostText("Blacksmith")}";
		HireButton.Text = $"Нанять жителя ({GameManager.CostText("Worker")})";
		SwordButton.Text = $"Выковать меч ({GameManager.CostText("Sword")})";
		BossButton.Text = $"Призвать каменного гиганта ({GameManager.CostText("Boss")})";

		foreach (string kind in GameManager.Kinds) JobOption.AddItem(GameManager.KindTitles[kind]);
		JobOption.Selected = System.Array.IndexOf(GameManager.Kinds, GameManager.Instance.WorkerJob);

		GameManager.Instance.ProgressChanged += Refresh;
		Refresh();
	}

	public override void _ExitTree()
	{
		GameManager.Instance.ProgressChanged -= Refresh;
	}

	public void Refresh()
	{
		GameManager game = GameManager.Instance;
		TownHallButton.Disabled = game.HasBuilding("TownHall");
		BlacksmithButton.Disabled = game.HasBuilding("Blacksmith") || !game.HasBuilding("TownHall");
		WorkersLabel.Text = $"Жители: {game.Workers.Count}/{GameManager.MaxWorkers}";
		HireButton.Disabled = !game.HasBuilding("TownHall") || game.Workers.Count >= GameManager.MaxWorkers;
		SwordButton.Disabled = game.SwordForged || !game.HasBuilding("Blacksmith");
		BossButton.Disabled = !game.SwordForged || IsInstanceValid(game.Boss);
	}

	public void OpenMenu()
	{
		Visible = true;
	}

	public void CloseMenu()
	{
		Visible = false;
	}

	public void _on_town_hall_pressed() => StartPlacement("TownHall");

	public void _on_blacksmith_pressed() => StartPlacement("Blacksmith");

	private void StartPlacement(string buildingId)
	{
		CloseMenu();
		Grid.StartPlacement(buildingId);
	}

	public void _on_hire_pressed() => GameManager.Instance.HireWorker();

	public void _on_job_selected(int index) => GameManager.Instance.SetWorkerJob(GameManager.Kinds[index]);

	public void _on_sword_pressed() => GameManager.Instance.ForgeSword();

	public void _on_boss_pressed()
	{
		CloseMenu();
		GameManager.Instance.SummonBoss();
	}
}
