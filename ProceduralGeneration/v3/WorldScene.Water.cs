using Godot;

public partial class WorldScene
{
	public const float OceanMargin = 16f;

	public WaterMap Water { get; private set; }

	private static ImageTexture shoreTexture;

	private void SetUpWater()
	{
		Water = new WaterMap(Map);
		shoreTexture = ImageTexture.CreateFromImage(Water.ShoreField());
		RenderingServer.GlobalShaderParameterSet("water_field", shoreTexture);
		RenderingServer.GlobalShaderParameterSet("water_field_size", (Vector2)(Map.Size * GenerationSettings.TILE_SIZE));
		RenderingServer.GlobalShaderParameterSet("water_trail_count", 0);

		AddChild(new WaterTrails { Name = "WaterTrails", World = this });
	}

	public bool IsOcean(Vector2 position) => Water != null && Water.KindAt(WaterMap.CellOf(position)) == WaterMap.Kind.Ocean;

	public bool IsShallow(Vector2 position) => Water != null && Water.KindAt(WaterMap.CellOf(position)) == WaterMap.Kind.Shallow;

	public Vector2 KeepOffOcean(Vector2 position, Vector2 velocity, double delta) =>
		Water?.Constrain(position, velocity, (float)delta, OceanMargin) ?? velocity;

	public Vector2 Walkable(Vector2 position) => Water?.Walkable(position) ?? position;

	public Vector2 SafeSpawn(Vector2 position) => Water?.SafeSpawn(position) ?? position;

	public Vector2 Steer(Vector2 from, Vector2 to) => Water?.Steer(from, to) ?? (to - from).Normalized();

	public Vector2 ReachableNear(Vector2 point, Vector2 goal) => Water?.ReachableNear(point, goal) ?? point;
}
