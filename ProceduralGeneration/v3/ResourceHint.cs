using Godot;

public partial class ResourceHint : CanvasLayer
{
	public WorldScene World;

	private PanelContainer panel;
	private Label title;
	private TextureRect icon;
	private Label amount;
	private string kind = "";

	public override void _Ready()
	{
		Layer = 4;
		ProcessMode = ProcessModeEnum.Always;
		panel = new PanelContainer { ThemeTypeVariation = "HudPanel", MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 2);
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 6);
		title = new Label { ThemeTypeVariation = "HudLabel" };
		icon = new TextureRect
		{
			CustomMinimumSize = new Vector2(20, 20),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		amount = new Label { ThemeTypeVariation = "HudLabel" };
		row.AddChild(icon);
		row.AddChild(amount);
		box.AddChild(title);
		box.AddChild(row);
		panel.AddChild(box);
		AddChild(panel);
	}

	public override void _Process(double delta) => ShowAt(World.GetGlobalMousePosition(), GetViewport().GetMousePosition());

	public void ShowAt(Vector2 point, Vector2 screen)
	{
		Vector2I cell = World.ResourceCellAt(point);
		WorldResources.Info info = WorldResources.Of(World.GetResourceAt(cell));
		panel.Visible = info != null && !GetTree().Paused && !Grid.buildMode && !OverGui();
		if (!panel.Visible) return;
		if (info.Kind != kind)
		{
			kind = info.Kind;
			icon.Texture = GD.Load<Texture2D>($"res://UI/Icons/{kind}.png");
		}
		title.Text = info.Title;
		amount.Text = $"{World.StockAt(cell)} / {info.Portions * info.Yield}";
		panel.ResetSize();
		Vector2 view = GetViewport().GetVisibleRect().Size;
		panel.Position = (screen + new Vector2(20, 20)).Clamp(Vector2.Zero, view - panel.Size);
	}

	private bool OverGui()
	{
		Control hovered = GetViewport().GuiGetHoveredControl();
		return hovered != null && hovered.MouseFilter == Control.MouseFilterEnum.Stop;
	}
}
