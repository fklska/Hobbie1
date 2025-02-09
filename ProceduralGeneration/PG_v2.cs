using Godot;
using System;
using System.Diagnostics;

public partial class PG_v2 : Node2D
{
    [ExportCategory("Regions")]
    [Export] public Color[] oceans;
    [Export] public Color beach;
    [Export] public Color[] grasses;
    [Export] public Color[] mountains;

    [Export] public Vector2I mapSize = new Vector2I(16, 16);
    [Export] public FastNoiseLite height_noise = new FastNoiseLite();
    [Export] public FastNoiseLite dry_noise = new FastNoiseLite();
    public int cellSize = 8;
    public int HalfCellSize = 4;

    float[,] noise_map;
    float[,] dry_noise_map;

    public override void _Ready()
    {
        noise_map = new float[mapSize.X, mapSize.Y];
        dry_noise_map = new float[mapSize.X, mapSize.Y];

        height_noise.Seed = Convert.ToInt32(GD.RandRange(0.0, 100000000));
        dry_noise.Seed = Convert.ToInt32(GD.RandRange(0.0, 100000000));
        Debug.WriteLine(generate_map());
    }

    public override void _Draw()
    {
        debug_drawMap();
    }

    public float[,] generate_map()
    {

       for (int i = 0; i < mapSize.X; i++)
       {
            for (int j = 0; j < mapSize.Y; j++)
            {
                float height_value = height_noise.GetNoise2D(i * cellSize + HalfCellSize, j*cellSize + HalfCellSize);
                float dry_value = dry_noise.GetNoise2D(i * cellSize + HalfCellSize, j * cellSize + HalfCellSize);

                noise_map[i,j] = height_value;
                dry_noise_map[i, j] = dry_value * height_value;
            }
       }

        return noise_map;
    }
    public void debug_drawMap()
    {
        for (int i = 0; i < mapSize.X; i++)
        {
            for (int j = 0; j < mapSize.Y; j++)
            {
                DrawRect(new Rect2(new Vector2(i,j)*cellSize, new Vector2(cellSize, cellSize)), currentBiom(noise_map[i, j], dry_noise_map[i, j]));
            }
        }
    }


    public Color currentBiom(float height, float dry)
    {
        if (height < 0)
        {
            if (dry > 0.2f)
            {
                return oceans[1];
            }
            return oceans[0];
        }

        if (height < 0.1f)
        {
            return beach;
        }

        if (height < 0.3f)
        {
            if (dry > 0.2f)
            {
                return grasses[1];
            }
            return grasses[0];
        }

        if (dry > 0.2f)
        {
            return mountains[1];
        }
        return mountains[0];
    }
}
