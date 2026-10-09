using Godot;
using System.Collections.Generic;

public partial class WaterTrails : Node
{
	public const int MaxStamps = 128;

	[Export] public float Life = 1.4f;
	[Export] public float StepDistance = 10f;
	[Export] public float IdleInterval = 1.2f;
	[Export] public float ViewMargin = 160f;

	public WorldScene World;

	private static readonly StringName[] Groups = { "village", "enemies" };

	private readonly Vector4[] stamps = new Vector4[MaxStamps];
	private readonly Dictionary<ulong, (Vector2 Position, double Time)> tracks = new();
	private readonly HashSet<ulong> seen = new();
	private static Image image;
	private static ImageTexture texture;
	private int next;
	private double clock;

	public override void _Ready()
	{
		for (int i = 0; i < MaxStamps; i++) stamps[i] = new Vector4(0, 0, -1000, 0);
		if (texture == null)
		{
			image = Image.CreateEmpty(MaxStamps, 1, false, Image.Format.Rgbaf);
			texture = ImageTexture.CreateFromImage(image);
		}
		RenderingServer.GlobalShaderParameterSet("water_trails", texture);
	}

	public override void _ExitTree()
	{
		RenderingServer.GlobalShaderParameterSet("water_trail_count", 0);
	}

	public override void _Process(double delta)
	{
		clock += delta;
		Viewport viewport = GetViewport();
		Rect2 view = (viewport.GetCanvasTransform().AffineInverse() * viewport.GetVisibleRect()).Grow(ViewMargin);

		seen.Clear();
		foreach (StringName group in Groups)
		{
			foreach (Node node in GetTree().GetNodesInGroup(group))
			{
				if (node is not CharacterBody2D body || !view.HasPoint(body.GlobalPosition)) continue;
				Track(body);
			}
		}
		if (tracks.Count > seen.Count)
		{
			var gone = new List<ulong>();
			foreach (ulong id in tracks.Keys) if (!seen.Contains(id)) gone.Add(id);
			foreach (ulong id in gone) tracks.Remove(id);
		}

		int count = 0;
		for (int i = 0; i < MaxStamps; i++)
		{
			Vector4 stamp = stamps[i];
			float age = (float)(clock - stamp.Z);
			if (age > Life) continue;
			image.SetPixel(count++, 0, new Color(stamp.X, stamp.Y, age / Life, stamp.W));
		}
		texture.Update(image);
		RenderingServer.GlobalShaderParameterSet("water_trail_count", count);
	}

	private void Track(CharacterBody2D body)
	{
		ulong id = body.GetInstanceId();
		Vector2 position = body.GlobalPosition;
		if (!World.IsShallow(position))
		{
			tracks.Remove(id);
			return;
		}
		seen.Add(id);
		Variant radius = body.Get("hit_radius");
		float size = radius.VariantType == Variant.Type.Nil ? 1f : Mathf.Clamp(radius.AsSingle() / 14f, 0.8f, 3f);

		if (!tracks.TryGetValue(id, out var last))
		{
			Stamp(position, -size);
			tracks[id] = (position, clock);
			return;
		}
		Vector2 step = position - last.Position;
		if (step.Length() >= StepDistance * size)
		{
			float heading = Mathf.Min(Mathf.PosMod(step.Angle(), Mathf.Tau) / Mathf.Tau, 0.999f);
			Stamp(position, Mathf.Floor(size * 16f) + heading);
			tracks[id] = (position, clock);
		}
		else if (clock - last.Time >= IdleInterval)
		{
			Stamp(position, -size);
			tracks[id] = (position, clock);
		}
	}

	private void Stamp(Vector2 position, float shape)
	{
		stamps[next] = new Vector4(position.X, position.Y, (float)clock, shape);
		next = (next + 1) % MaxStamps;
	}
}
