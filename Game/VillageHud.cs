using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class VillageHud : CanvasLayer
{
	private static readonly Color LockColor = new(1f, 0.55f, 0.45f);
	private static readonly Color DoneColor = new(0.62f, 0.9f, 0.55f);

	private GameManager game;
	private PanelContainer hireWindow;
	private VBoxContainer hireContent;
	private VBoxContainer hireBottom;
	private PanelContainer buildingPanel;
	private VBoxContainer buildingContent;
	private VBoxContainer buildingBottom;
	private GameManager.PlacedBuilding selected;
	private Node2D hovered;
	private bool pointerFree;
	private readonly List<Action> updaters = new();
	private double tickTimer;

	public override void _Ready()
	{
		game = GameManager.Instance;
		Layer = 3;
		Control root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		(hireWindow, hireContent, hireBottom) = AddWindow(root, Control.LayoutPreset.Center);
		hireWindow.OffsetLeft = -460;
		hireWindow.OffsetRight = 460;
		hireWindow.OffsetTop = -260;
		hireWindow.OffsetBottom = 260;

		(buildingPanel, buildingContent, buildingBottom) = AddWindow(root, Control.LayoutPreset.CenterRight);
		buildingPanel.OffsetLeft = -500;
		buildingPanel.OffsetRight = -20;
		buildingPanel.OffsetTop = -270;
		buildingPanel.OffsetBottom = 270;
		buildingPanel.VisibilityChanged += () =>
		{
			if (!buildingPanel.Visible) Deselect();
		};

		game.ProgressChanged += Update;
		game.StockChanged += Update;
	}

	public override void _ExitTree()
	{
		game.ProgressChanged -= Update;
		game.StockChanged -= Update;
	}

	private static (PanelContainer, VBoxContainer, VBoxContainer) AddWindow(Control root, Control.LayoutPreset preset)
	{
		PanelContainer window = new() { Visible = false };
		window.AddToGroup("closable_ui");
		window.SetAnchorsPreset(preset);
		window.GrowHorizontal = preset == Control.LayoutPreset.Center ? Control.GrowDirection.Both : Control.GrowDirection.Begin;
		window.GrowVertical = Control.GrowDirection.Both;
		root.AddChild(window);
		VBoxContainer frame = new();
		frame.AddThemeConstantOverride("separation", 8);
		window.AddChild(frame);
		ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		frame.AddChild(scroll);
		VBoxContainer content = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		content.AddThemeConstantOverride("separation", 8);
		scroll.AddChild(content);
		VBoxContainer bottom = new();
		bottom.AddThemeConstantOverride("separation", 8);
		frame.AddChild(bottom);
		return (window, content, bottom);
	}

	private void CloseOthers()
	{
		foreach (Node node in GetTree().GetNodesInGroup("closable_ui"))
			if (node is Control control && control != hireWindow && control != buildingPanel) control.Visible = false;
	}

	public void ShowHire()
	{
		CloseOthers();
		buildingPanel.Visible = false;
		BuildHire();
		hireWindow.Visible = true;
	}

	private void Update()
	{
		if (!hireWindow.Visible && !buildingPanel.Visible) return;
		foreach (Action update in updaters) update();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion) pointerFree = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			pointerFree = true;
			return;
		}
		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } || !CanPick()) return;
		GameManager.PlacedBuilding placed = BuildingAt(game.World.GetGlobalMousePosition());
		if (placed == null) return;
		SoundManager.Instance.Play("ui_click");
		if (placed.Id == Economy.Core) ShowHire();
		else Select(placed);
		GetViewport().SetInputAsHandled();
	}

	public override void _Process(double delta)
	{
		UpdateHover();
		if (buildingPanel.Visible && (selected == null || !IsInstanceValid(selected.Node))) buildingPanel.Visible = false;
		tickTimer -= delta;
		if (tickTimer > 0) return;
		tickTimer = 0.5;
		Update();
	}

	private bool CanPick() => !Grid.buildMode && !game.Ended && !game.ChoosingClass && IsInstanceValid(game.World);

	private void UpdateHover()
	{
		Node2D next = CanPick() && pointerFree ? BuildingAt(game.World.GetGlobalMousePosition())?.Node : null;
		if (next == hovered) return;
		if (IsInstanceValid(hovered) && hovered != selected?.Node) hovered.Call("hide_outline");
		hovered = next;
		if (IsInstanceValid(hovered)) hovered.Call("set_outline");
	}

	private GameManager.PlacedBuilding BuildingAt(Vector2 point)
	{
		bool resource = game.World.GetResourceAt(game.World.ResourceCellAt(point)) != ResorseType.None;
		GameManager.PlacedBuilding best = null;
		float bestY = float.MinValue;
		foreach (GameManager.PlacedBuilding p in game.Placed)
		{
			if (!IsInstanceValid(p.Node)) continue;
			Rect2 foot = new(p.Cell * GenerationSettings.TILE_SIZE, Economy.Footprint(p.Id, p.Turn) * GenerationSettings.TILE_SIZE);
			bool hit = foot.HasPoint(point) || !resource && p.Node.Call("hit_rect").AsRect2().HasPoint(point);
			if (!hit || foot.End.Y <= bestY) continue;
			best = p;
			bestY = foot.End.Y;
		}
		return best;
	}

	private void Select(GameManager.PlacedBuilding placed)
	{
		CloseOthers();
		hireWindow.Visible = false;
		Deselect();
		selected = placed;
		selected.Node.Call("set_outline");
		BuildBuilding();
		buildingPanel.Visible = true;
	}

	private void Deselect()
	{
		if (selected != null && IsInstanceValid(selected.Node) && selected.Node != hovered) selected.Node.Call("hide_outline");
		selected = null;
	}

	private void Clear(params VBoxContainer[] boxes)
	{
		updaters.Clear();
		foreach (VBoxContainer box in boxes)
			foreach (Node child in box.GetChildren())
			{
				box.RemoveChild(child);
				child.QueueFree();
			}
	}

	private static Label AddLabel(Container parent, string text = "", string variation = "", Color? color = null)
	{
		Label label = new() { Text = text, ThemeTypeVariation = variation, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		if (color != null) label.AddThemeColorOverride("font_color", color.Value);
		parent.AddChild(label);
		return label;
	}

	private static Button AddButton(Container parent, Action pressed, bool primary = false)
	{
		Button button = new() { FocusMode = Control.FocusModeEnum.None, ThemeTypeVariation = primary ? "PrimaryButton" : "" };
		button.Pressed += pressed;
		parent.AddChild(button);
		return button;
	}

	private static void SetLock(Label label, string text)
	{
		label.Text = text;
		label.Visible = text != "";
	}

	private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text[1..];

	private HBoxContainer AddFooter(VBoxContainer bottom, Action close)
	{
		HBoxContainer footer = new() { Alignment = BoxContainer.AlignmentMode.End };
		footer.AddThemeConstantOverride("separation", 8);
		bottom.AddChild(footer);
		AddButton(footer, close).Text = "Закрыть";
		return footer;
	}

	private void AddUpgrade(VBoxContainer bottom, HBoxContainer footer, string id, string prefix)
	{
		Label lockLabel = AddLabel(bottom, color: LockColor);
		bottom.MoveChild(lockLabel, footer.GetIndex());
		Button upgrade = AddButton(footer, () => game.UpgradeBuilding(id));
		upgrade.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		upgrade.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		footer.MoveChild(upgrade, 0);
		updaters.Add(() =>
		{
			Economy.Level next = game.NextLevel(id);
			string reason = next == null ? "" : game.UpgradeLock(id);
			int times = game.UpgradeTimes(id);
			upgrade.Visible = next != null;
			SetLock(lockLabel, reason == "" ? "" : $"Для улучшения: {char.ToLower(reason[0])}{reason[1..]}");
			if (next == null) return;
			upgrade.Text = $"{prefix} «{next.Title}» ({Economy.CostText(next.Cost, times)})";
			upgrade.TooltipText = Capitalize(Economy.EffectText(next));
			upgrade.Disabled = reason != "" || !game.CanAfford(next.Cost, times);
		});
	}

	private void BuildHire()
	{
		Clear(hireContent, hireBottom);
		AddLabel(hireContent, "Нанять жителей", "HeaderLabel").HorizontalAlignment = HorizontalAlignment.Center;
		Label info = AddLabel(hireContent, variation: "SubtleLabel");
		Label lockLabel = AddLabel(hireContent, color: LockColor);
		updaters.Add(() =>
		{
			info.Text = $"{game.LevelInfo(Economy.Core).Title}: жители {game.Workers.Count}/{game.PopulationCap}. Еда {game.GetStock("food")}, урожай +{game.FoodPerDay}, " +
				$"каждое утро житель съедает {Economy.FoodPerWorker}. Добычу носят в ближайшее своё здание или в центр";
			string reason = game.HireLock();
			SetLock(lockLabel, reason != "" ? reason : game.Hungry ? "Жители голодают и работают вдвое медленнее" : "");
		});

		HBoxContainer cards = new();
		cards.AddThemeConstantOverride("separation", 12);
		hireContent.AddChild(cards);
		foreach (var (id, profession) in Economy.Professions) cards.AddChild(HireCard(id, profession));

		HBoxContainer footer = AddFooter(hireBottom, () => hireWindow.Visible = false);
		AddUpgrade(hireBottom, footer, Economy.Core, "Улучшить центр до");
		Update();
	}

	private Control HireCard(string id, Economy.Profession profession)
	{
		PanelContainer card = new() { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(280, 0) };
		VBoxContainer box = new();
		box.AddThemeConstantOverride("separation", 6);
		card.AddChild(box);
		box.AddChild(new TextureRect
		{
			Texture = GD.Load<SpriteFrames>(profession.Frames).GetFrameTexture("idle_s", 0),
			CustomMinimumSize = new Vector2(0, 140),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		});
		AddLabel(box, profession.Title, "SubheaderLabel").HorizontalAlignment = HorizontalAlignment.Center;
		AddLabel(box, profession.Description, "SubtleLabel").CustomMinimumSize = new Vector2(260, 0);
		Label stats = AddLabel(box);
		box.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
		Button hire = AddButton(box, () => game.Hire(id), true);
		hire.Text = $"Нанять ({Economy.CostText(profession.Cost)})";

		string kinds = string.Join(", ", profession.Kinds.Select(k => Economy.ResourceTitles[k].ToLower()));
		updaters.Add(() =>
		{
			string building = game.HasBuilding(profession.Building)
				? $"{game.LevelInfo(profession.Building).Title}: улучшений {game.Researched.GetValueOrDefault(profession.Building)}/{Economy.Researches[profession.Building].Length}"
				: $"Нет здания: {Economy.Buildings[profession.Building].Levels[0].Title.ToLower()}";
			stats.Text = $"Добывает: {kinds}\nРаботают: {game.ProfessionCount(id)}, скорость ×{game.JobSpeed(id):0.##}\n{building}";
			hire.Disabled = game.HireLock() != "" || !game.CanAfford(profession.Cost);
		});
		return card;
	}

	private void BuildBuilding()
	{
		Clear(buildingContent, buildingBottom);
		string id = selected.Id;
		Node2D node = selected.Node;
		Economy.BuildingInfo info = Economy.Buildings[id];

		HBoxContainer head = new();
		head.AddThemeConstantOverride("separation", 10);
		buildingContent.AddChild(head);
		TextureRect icon = new()
		{
			CustomMinimumSize = new Vector2(72, 72),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		};
		head.AddChild(icon);
		VBoxContainer titles = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		head.AddChild(titles);
		Label title = AddLabel(titles, variation: "SubheaderLabel");
		Label hp = AddLabel(titles, variation: "SubtleLabel");
		Label about = AddLabel(buildingContent, variation: "SubtleLabel");
		updaters.Add(() =>
		{
			if (!IsInstanceValid(node)) return;
			Economy.Level level = game.LevelInfo(id);
			icon.Texture = node.Call("get_texture").As<Texture2D>();
			title.Text = $"{level.Title} (ур. {game.GetLevel(id)}/{info.Levels.Length})";
			hp.Text = $"Прочность {node.Get("hp")}/{node.Get("max_hp")}";
			about.Text = $"{info.Description}. {Capitalize(Economy.EffectText(level))}";
		});

		var profession = Economy.Professions.FirstOrDefault(p => p.Value.Building == id);
		if (profession.Value != null) AddResearch(id, profession.Key, profession.Value);

		HBoxContainer footer = AddFooter(buildingBottom, () => buildingPanel.Visible = false);
		AddUpgrade(buildingBottom, footer, id, "Улучшить до");
		Update();
	}

	private void AddResearch(string id, string jobId, Economy.Profession profession)
	{
		AddLabel(buildingContent, "Улучшения добычи", "HeaderLabel").HorizontalAlignment = HorizontalAlignment.Center;
		Label workers = AddLabel(buildingContent, variation: "SubtleLabel");
		updaters.Add(() => workers.Text = $"{profession.Plural}: {game.ProfessionCount(jobId)}, скорость ×{game.JobSpeed(jobId):0.##}. " +
			"Улучшение идёт несколько дней, следующее открывается с уровнем здания. Нанимают жителей в центре поселения");

		Economy.Research[] list = Economy.Researches[id];
		for (int i = 0; i < list.Length; i++)
		{
			int index = i;
			Economy.Research research = list[i];
			PanelContainer row = new() { ThemeTypeVariation = "InsetPanel" };
			buildingContent.AddChild(row);
			VBoxContainer box = new();
			box.AddThemeConstantOverride("separation", 4);
			row.AddChild(box);
			Label name = AddLabel(box, research.Title, "SubheaderLabel");
			AddLabel(box, $"{profession.Plural} +{Mathf.RoundToInt(research.Bonus * 100)}% к скорости, {Economy.DaysText(research.Days)}", "SubtleLabel");
			ProgressBar bar = new() { ThemeTypeVariation = "GreenBar", MaxValue = research.Days, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 14) };
			box.AddChild(bar);
			Label status = AddLabel(box);
			Button start = AddButton(box, () => game.StartResearch(id), true);
			start.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			start.Text = $"Начать ({Economy.CostText(research.Cost)})";

			updaters.Add(() =>
			{
				int done = game.Researched.GetValueOrDefault(id);
				bool current = index == done;
				bool running = current && game.Researching(id);
				string reason = current && !running ? game.ResearchLock(id) : "";
				name.Text = index < done ? $"✓ {research.Title}" : research.Title;
				name.RemoveThemeColorOverride("font_color");
				if (index < done) name.AddThemeColorOverride("font_color", DoneColor);
				bar.Visible = running;
				start.Visible = current && !running;
				start.Disabled = reason != "" || !game.CanAfford(research.Cost);
				status.RemoveThemeColorOverride("font_color");
				if (running)
				{
					float left = game.ResearchLeft(id);
					bar.Value = research.Days - left;
					status.Text = $"Идёт улучшение, осталось {left:0.#} дн.";
				}
				else if (index > done) status.Text = "Сначала изучите предыдущее улучшение";
				else status.Text = reason;
				if (!running && status.Text != "") status.AddThemeColorOverride("font_color", LockColor);
				status.Visible = index >= done && status.Text != "";
			});
		}
	}
}
