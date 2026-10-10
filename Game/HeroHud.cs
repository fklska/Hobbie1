using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class HeroHud : CanvasLayer
{
	private static readonly Dictionary<string, Texture2D> icons = new();
	private static readonly Color LockColor = new(1f, 0.55f, 0.45f);
	private static readonly Color RankColor = new(0.965f, 0.824f, 0.471f);

	private GameManager game;
	private HBoxContainer bar;
	private readonly AbilitySlot[] slots = new AbilitySlot[3];
	private Button treeButton;
	private Label badge;
	private PanelContainer window;
	private VBoxContainer content;
	private bool rebuild;

	public static Texture2D SkillIcon(string id)
	{
		if (icons.TryGetValue(id, out Texture2D icon)) return icon;
		return icons[id] = GD.Load<Texture2D>($"res://UI/Icons/skills/{id}.png");
	}

	public override void _Ready()
	{
		game = GameManager.Instance;
		Layer = 3;

		Control root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		bar = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		bar.AddThemeConstantOverride("separation", 10);
		bar.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
		bar.GrowHorizontal = Control.GrowDirection.Begin;
		bar.GrowVertical = Control.GrowDirection.Begin;
		bar.OffsetLeft = bar.OffsetRight = -20;
		bar.OffsetTop = bar.OffsetBottom = -20;
		root.AddChild(bar);
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i] = new AbilitySlot { Slot = i, TooltipText = " " };
			bar.AddChild(slots[i]);
		}

		treeButton = GD.Load<PackedScene>("res://UI/prefabs/StoreButton.tscn").Instantiate<Button>();
		treeButton.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		treeButton.OffsetLeft = 160;
		treeButton.OffsetRight = 224;
		treeButton.OffsetTop = -80;
		treeButton.OffsetBottom = -16;
		treeButton.GrowVertical = Control.GrowDirection.Begin;
		treeButton.TooltipText = "Способности (K)";
		treeButton.GetNode<Label>("Key").Text = "K";
		root.AddChild(treeButton);
		badge = new Label { ThemeTypeVariation = "HudLabel", HorizontalAlignment = HorizontalAlignment.Center };
		badge.AddThemeColorOverride("font_color", RankColor);
		badge.Position = new Vector2(40, -8);
		badge.Size = new Vector2(30, 24);
		treeButton.AddChild(badge);

		BuildWindow(root);
		treeButton.Set("target", treeButton.GetPathTo(window));

		game.HeroChanged += Refresh;
		game.ProgressChanged += Refresh;
		Refresh();
	}

	public override void _ExitTree()
	{
		game.HeroChanged -= Refresh;
		game.ProgressChanged -= Refresh;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("skills") || game.ClassInfo == null) return;
		window.Visible = !window.Visible;
		GetViewport().SetInputAsHandled();
	}

	public void FlashSlot(int slot)
	{
		if (slot >= 0 && slot < slots.Length) slots[slot].Flash();
	}

	private void BuildWindow(Control root)
	{
		window = new PanelContainer { Visible = false };
		window.AddToGroup("closable_ui");
		window.SetAnchorsPreset(Control.LayoutPreset.Center);
		window.GrowHorizontal = Control.GrowDirection.Both;
		window.GrowVertical = Control.GrowDirection.Both;
		window.OffsetLeft = -400;
		window.OffsetRight = 400;
		window.OffsetTop = -300;
		window.OffsetBottom = 300;
		root.AddChild(window);
		ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		window.AddChild(scroll);
		content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		content.AddThemeConstantOverride("separation", 8);
		scroll.AddChild(content);
		window.VisibilityChanged += Refresh;
	}

	private void Refresh()
	{
		HeroClasses.ClassInfo info = game.ClassInfo;
		bar.Visible = info != null;
		treeButton.Visible = info != null;
		if (info == null)
		{
			window.Visible = false;
			return;
		}
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i].Skill = info.Skills.FirstOrDefault(s => s.Slot == i);
			slots[i].Visible = slots[i].Skill != null;
		}
		treeButton.Icon = GD.Load<Texture2D>($"res://UI/Icons/skills/class_{info.Id}.png");
		bool canLearn = info.Skills.Any(s => game.LearnLock(s.Id) == "");
		badge.Text = canLearn ? game.SkillPoints.ToString() : "";
		rebuild = window.Visible;
	}

	public override void _Process(double delta)
	{
		if (!rebuild) return;
		rebuild = false;
		if (game.ClassInfo != null) RebuildTree(game.ClassInfo);
	}

	private void RebuildTree(HeroClasses.ClassInfo info)
	{
		foreach (Node child in content.GetChildren())
		{
			content.RemoveChild(child);
			child.QueueFree();
		}

		HBoxContainer head = new();
		head.AddThemeConstantOverride("separation", 10);
		head.AddChild(new TextureRect
		{
			Texture = SkillIcon($"class_{info.Id}"),
			CustomMinimumSize = new Vector2(48, 48),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		});
		VBoxContainer titles = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		titles.AddChild(new Label { Text = $"{info.Title}: способности", ThemeTypeVariation = "HeaderLabel" });
		HBoxContainer points = new();
		points.AddThemeConstantOverride("separation", 6);
		points.AddChild(new TextureRect
		{
			Texture = SkillIcon("skill_point"),
			CustomMinimumSize = new Vector2(24, 24),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		});
		points.AddChild(new Label
		{
			Text = $"Свободных очков: {game.SkillPoints}. Новое очко каждый рассвет.",
			ThemeTypeVariation = "SubtleLabel",
		});
		titles.AddChild(points);
		head.AddChild(titles);
		Button close = new() { Text = "Закрыть", FocusMode = Control.FocusModeEnum.None, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		close.Pressed += () => window.Hide();
		head.AddChild(close);
		content.AddChild(head);
		content.AddChild(new HSeparator());

		HBoxContainer columns = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		columns.AddThemeConstantOverride("separation", 12);
		content.AddChild(columns);
		for (int b = 0; b < info.Branches.Length; b++)
		{
			VBoxContainer column = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			column.AddThemeConstantOverride("separation", 8);
			column.AddChild(new Label { Text = info.Branches[b], ThemeTypeVariation = "SubheaderLabel", HorizontalAlignment = HorizontalAlignment.Center });
			foreach (HeroClasses.Skill skill in info.Skills.Where(s => s.Branch == b).OrderBy(s => s.Tier))
				column.AddChild(SkillCard(skill));
			columns.AddChild(column);
		}
	}

	private Control SkillCard(HeroClasses.Skill skill)
	{
		int rank = game.Rank(skill.Id);
		PanelContainer card = new() { ThemeTypeVariation = "InsetPanel" };
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 10);
		card.AddChild(row);
		row.AddChild(new TextureRect
		{
			Texture = SkillIcon(skill.Id),
			CustomMinimumSize = new Vector2(48, 48),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
			Modulate = rank > 0 ? Colors.White : new Color(0.5f, 0.48f, 0.46f),
		});
		VBoxContainer text = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddChild(text);
		string key = skill.Active ? $" · {HeroClasses.SlotKeys[skill.Slot]}" : skill.Id == game.ClassInfo.Innate && !skill.Active ? " · атака" : "";
		text.AddChild(new Label { Text = $"{skill.Title}{key}", ThemeTypeVariation = "SubheaderLabel" });
		Label rankLabel = new() { Text = $"Ранг {rank}/{skill.MaxRank}" };
		rankLabel.AddThemeColorOverride("font_color", RankColor);
		text.AddChild(rankLabel);
		text.AddChild(new Label
		{
			Text = skill.Description,
			ThemeTypeVariation = "SubtleLabel",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(250, 0),
		});
		if (rank > 0) text.AddChild(new Label { Text = $"Сейчас: {skill.Ranks[rank - 1]}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
		if (rank < skill.MaxRank) text.AddChild(new Label { Text = $"{(rank == 0 ? "Даёт" : "Дальше")}: {skill.Ranks[rank]}", ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart });

		string reason = game.LearnLock(skill.Id);
		if (rank < skill.MaxRank)
		{
			if (reason != "" && !reason.StartsWith("Нужно"))
			{
				Label lockLabel = new() { Text = reason, AutowrapMode = TextServer.AutowrapMode.WordSmart };
				lockLabel.AddThemeColorOverride("font_color", LockColor);
				text.AddChild(lockLabel);
			}
			int cost = skill.Costs[rank];
			Button learn = new()
			{
				Text = $"{(rank == 0 ? "Изучить" : "Улучшить")} · {cost} {Points(cost)}",
				ThemeTypeVariation = reason == "" ? "PrimaryButton" : "",
				Disabled = reason != "",
				FocusMode = Control.FocusModeEnum.None,
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
			};
			learn.Pressed += () =>
			{
				if (game.LearnSkill(skill.Id) && skill.Active) FlashSlot(skill.Slot);
			};
			text.AddChild(learn);
		}
		return card;
	}

	private static string Points(int n) => (n % 10, n % 100) switch
	{
		(1, not 11) => "очко",
		(2 or 3 or 4, not (12 or 13 or 14)) => "очка",
		_ => "очков",
	};
}
