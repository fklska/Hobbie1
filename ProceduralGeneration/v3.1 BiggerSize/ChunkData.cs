using Godot;

[GlobalClass]
[Tool]
public partial class ChunkData : Resource
{
    [Export] public bool Active;

    [Export] public Vector2I GlobalCoord;
    [Export] public Godot.Collections.Array<Godot.Collections.Array<Tile>> Map;
    [Export] public Vector4I rect;
    public Godot.Collections.Array<Node2D> Resourses = new Godot.Collections.Array<Node2D>();

    public void ResetData()
    {
        Map = InitializeArray();
        rect.X = GlobalCoord.X * GenerationSettings.CHUNK_SIZE;
        rect.Y = GlobalCoord.Y * GenerationSettings.CHUNK_SIZE;
        rect.Z = GlobalCoord.X * GenerationSettings.CHUNK_SIZE + GenerationSettings.CHUNK_SIZE;
        rect.W = GlobalCoord.Y * GenerationSettings.CHUNK_SIZE + GenerationSettings.CHUNK_SIZE;
    }

    public Godot.Collections.Array<Godot.Collections.Array<Tile>> InitializeArray()
    {
        Godot.Collections.Array<Godot.Collections.Array<Tile>> Map = new Godot.Collections.Array<Godot.Collections.Array<Tile>>();

        Map.Resize(GenerationSettings.CHUNK_SIZE);
        for (int x = 0; x < GenerationSettings.CHUNK_SIZE; x++)
        {
            Map[x] = new Godot.Collections.Array<Tile>();
            Map[x].Resize(GenerationSettings.CHUNK_SIZE);

            for (int y = 0; y < GenerationSettings.CHUNK_SIZE; y++)
            {
                Map[x][y] = new Tile();
            }
        }
        return Map;
    }
}
