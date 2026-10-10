using Godot;

public partial class AbilitySlot : Control
{
	private static readonly Color Back = new(0.094f, 0.07f, 0.059f, 0.92f);
	private static readonly Color Ring = new(0.502f, 0.353f, 0.149f);
	private static readonly Color Ready = new(0.965f, 0.824f, 0.471f);
	private static readonly Color Shade = new(0f, 0f, 0f, 0.62f);

	public int Slot;
	public HeroClasses.Skill Skill;

	private Label key;
	private Label timer;
	private float flash;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(64, 64);
		MouseFilter = MouseFilterEnum.Stop;
		key = new Label { Text = HeroClasses.SlotKeys[Slot], ThemeTypeVariation = "HudLabel", HorizontalAlignment = HorizontalAlignment.Right };
		key.AddThemeFontSizeOverride("font_size", 15);
		key.SetAnchorsPreset(LayoutPreset.BottomRight);
		key.Position = new Vector2(30, 42);
		key.Size = new Vector2(32, 22);
		AddChild(key);
		timer = new Label { ThemeTypeVariation = "HudLabel", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		timer.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(timer);
	}

	public override void _Process(double delta)
	{
		flash = Mathf.Max(0, flash - (float)delta * 3);
		QueueRedraw();
		PlayerMainCharacter player = GameManager.Instance.Player;
		float left = Skill != null && IsInstanceValid(player) ? (float)player.CooldownLeft(Skill.Id) : 0;
		timer.Text = left > 0 ? Mathf.CeilToInt(left).ToString() : "";
	}

	public void Flash() => flash = 1;

	public override void _Draw()
	{
		Vector2 c = Size / 2;
		float r = Mathf.Min(Size.X, Size.Y) / 2 - 2;
		GameManager game = GameManager.Instance;
		PlayerMainCharacter player = game.Player;
		int rank = Skill == null ? 0 : game.Rank(Skill.Id);
		float left = Skill != null && IsInstanceValid(player) ? (float)player.CooldownLeft(Skill.Id) : 0;
		float total = Skill != null && IsInstanceValid(player) ? player.CooldownOf(Skill) : 1;

		DrawCircle(c, r + 2, new Color(0.11f, 0.075f, 0.06f));
		DrawCircle(c, r, Back);
		if (Skill != null)
		{
			Texture2D icon = HeroHud.SkillIcon(Skill.Id);
			Color tint = rank == 0 ? new Color(0.35f, 0.33f, 0.32f) : Colors.White;
			DrawTextureRect(icon, new Rect2(c - new Vector2(24, 24), new Vector2(48, 48)), false, tint);
		}
		if (left > 0 && total > 0)
		{
			float part = Mathf.Clamp(left / total, 0, 1);
			int steps = Mathf.Max(3, (int)(48 * part));
			Vector2[] pie = new Vector2[steps + 2];
			pie[0] = c;
			for (int i = 0; i <= steps; i++)
			{
				float a = -Mathf.Pi / 2 + Mathf.Tau * (1 - part) + Mathf.Tau * part * i / steps;
				pie[i + 1] = c + Vector2.FromAngle(a) * r;
			}
			DrawColoredPolygon(pie, Shade);
		}
		bool ready = rank > 0 && left <= 0;
		DrawArc(c, r, 0, Mathf.Tau, 48, ready ? Ready : Ring, 2.5f, true);
		if (flash > 0) DrawArc(c, r + 3, 0, Mathf.Tau, 48, new Color(Ready, flash), 3f, true);
		for (int i = 0; i < (Skill?.MaxRank ?? 0); i++)
		{
			Vector2 p = c + Vector2.FromAngle(Mathf.Pi * (0.75f + 0.12f * i)) * (r - 1);
			DrawCircle(p, 3.2f, new Color(0.11f, 0.075f, 0.06f));
			DrawCircle(p, 2.2f, i < rank ? Ready : new Color(0.33f, 0.27f, 0.22f));
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
		AcceptEvent();
		PlayerMainCharacter player = GameManager.Instance.Player;
		if (IsInstanceValid(player)) player.UseSlot(Slot, true);
	}

	public override string _GetTooltip(Vector2 atPosition)
	{
		if (Skill == null) return "";
		int rank = GameManager.Instance.Rank(Skill.Id);
		string state = rank == 0 ? "Не изучено" : $"Ранг {rank}/{Skill.MaxRank}: {Skill.Ranks[rank - 1]}";
		return $"{Skill.Title} ({HeroClasses.SlotKeys[Slot]})\n{Skill.Description}\n{state}";
	}
}
