using Godot;
using System.Linq;

public partial class ClassPicker : CanvasLayer
{
	public override void _Ready()
	{
		Layer = 6;
		ProcessMode = ProcessModeEnum.Always;
		GetTree().Paused = true;

		Control root = new() { MouseFilter = Control.MouseFilterEnum.Stop };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);
		ColorRect dim = new() { Color = new Color(0.05f, 0.03f, 0.02f, 0.72f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);
		CenterContainer center = new();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(center);

		PanelContainer panel = new();
		center.AddChild(panel);
		VBoxContainer box = new();
		box.AddThemeConstantOverride("separation", 10);
		panel.AddChild(box);
		box.AddChild(new Label { Text = "Выберите класс героя", ThemeTypeVariation = "HeaderLabel", HorizontalAlignment = HorizontalAlignment.Center });
		box.AddChild(new Label
		{
			Text = "Класс выбирается на всю партию. Первая способность уже изучена, каждый рассвет даёт очко для дерева способностей (K).",
			ThemeTypeVariation = "SubtleLabel",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(820, 0),
		});
		HBoxContainer cards = new();
		cards.AddThemeConstantOverride("separation", 12);
		box.AddChild(cards);
		foreach (HeroClasses.ClassInfo info in HeroClasses.Classes.Values) cards.AddChild(Card(info));
	}

	public override void _Process(double delta)
	{
		if (!IsQueuedForDeletion() && !GetTree().Paused) GetTree().Paused = true;
	}

	private static Control Card(HeroClasses.ClassInfo info)
	{
		PanelContainer card = new() { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(270, 0) };
		VBoxContainer box = new();
		box.AddThemeConstantOverride("separation", 6);
		card.AddChild(box);

		SpriteFrames frames = GD.Load<SpriteFrames>(info.Frames);
		box.AddChild(new TextureRect
		{
			Texture = frames.GetFrameTexture("idle_s", 0),
			CustomMinimumSize = new Vector2(0, 150),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		});
		box.AddChild(new Label { Text = info.Title, ThemeTypeVariation = "SubheaderLabel", HorizontalAlignment = HorizontalAlignment.Center });
		box.AddChild(new Label
		{
			Text = info.Description,
			ThemeTypeVariation = "SubtleLabel",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(250, 0),
		});

		foreach (HeroClasses.Skill skill in info.Skills.Where(s => s.Active || s.Id == info.Innate).OrderBy(s => s.Slot))
		{
			HBoxContainer row = new();
			row.AddThemeConstantOverride("separation", 6);
			row.AddChild(new TextureRect
			{
				Texture = HeroHud.SkillIcon(skill.Id),
				CustomMinimumSize = new Vector2(32, 32),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			});
			string key = skill.Active ? HeroClasses.SlotKeys[skill.Slot] : "атака";
			row.AddChild(new Label { Text = $"{skill.Title} ({key})", SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
			box.AddChild(row);
		}

		Control spacer = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		box.AddChild(spacer);
		Button choose = new() { Text = "Выбрать", ThemeTypeVariation = "PrimaryButton", FocusMode = Control.FocusModeEnum.None };
		choose.Pressed += () => GameManager.Instance.ChooseClass(info.Id);
		box.AddChild(choose);
		return card;
	}
}
