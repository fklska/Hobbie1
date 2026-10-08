using Godot;

public partial class BuildMenu : TabContainer
{
	[Export] public Button TownHallButton;
	[Export] public Button BlacksmithButton;
	[Export] public Label WorkersLabel;
	[Export] public Button HireButton;
	[Export] public OptionButton JobOption;
	[Export] public Button SwordButton;
	[Export] public Button ArmMeleeButton;
	[Export] public Button ArmRangedButton;

	public override void _Ready()
	{
		SetTabTitle(0, "Постройки");
		SetTabTitle(1, "Деревня");
		TownHallButton.Text = $"Ратуша\n{GameManager.CostText("TownHall")}";
		BlacksmithButton.Text = $"Кузница\n{GameManager.CostText("Blacksmith")}";
		HireButton.Text = $"Нанять жителя ({GameManager.CostText("Worker")})";
		SwordButton.Text = $"Выковать меч ({GameManager.CostText("Sword")})";
		ArmMeleeButton.Text = $"Вооружить жителя копьём ({GameManager.CostText("spear")})";
		ArmRangedButton.Text = $"Вооружить жителя луком ({GameManager.CostText("bow")})";

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
		WorkersLabel.Text = $"Жители: {game.Workers.Count}/{GameManager.MaxWorkers}, с оружием: {game.ArmedWorkers}";
		HireButton.Disabled = !game.HasBuilding("TownHall") || game.Workers.Count >= GameManager.MaxWorkers;
		SwordButton.Disabled = game.SwordForged || !game.HasBuilding("Blacksmith");
		ArmMeleeButton.Disabled = game.ArmedWorkers >= game.Workers.Count;
		ArmRangedButton.Disabled = ArmMeleeButton.Disabled;
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

	public void _on_arm_melee_pressed() => GameManager.Instance.ArmNextWorker("spear");

	public void _on_arm_ranged_pressed() => GameManager.Instance.ArmNextWorker("bow");
}
