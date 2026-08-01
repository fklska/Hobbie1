using Godot;
using Godot1.Globals;
using System;
using System.Collections.Generic;

public partial class Grid : Node2D
{
	[Export] Vector2I cellSize;
	[Export] Vector2I gridSize;
	[Export] Color defaultCellColor;
	[Export] Color StaticCellColor;
	[Export] Color defaultLineColor;
	[Export] Vector2I staticGridSize;

	Vector2I currentCell = Vector2I.Zero;
	bool drawOnce = true;
	public static bool buildMode = false;

	public WorldScene WorldScene;
	public override void _Ready()
	{
		WorldScene = GetTree().Root.GetNode<WorldScene>("/root/Map");
	}

	public override void _Process(double delta)
	{
		if (buildMode)
		{
			if (currentCell != pixelToCell(GetGlobalMousePosition()))
			{
				QueueRedraw();
			}
		}
	}

	public HashSet<TileType> BanTilesToPlace = new HashSet<TileType>()
	{
		TileType.DeepWater,
		TileType.TropicWater
	};

public bool IsAbleToPlace(Vector2I cell)
	{
		Vector2I localCell = Utils.GetLocalCell(globalCell: cell);
		Vector2I chunk = Utils.GetChunkCoords(cell);

		Tile tile = WorldScene.GeneratorData.ChunkMap[chunk.X][chunk.Y].Map[localCell.X][localCell.Y];

		return !BanTilesToPlace.Contains(tile.Type) && tile.Resourse == ResorseType.None;
	}

	public override void _Draw()
	{
		drawGridNearMouse();
		//drawStaticGrid();
		// drawLineGridNearMouse();
	}

	public void drawGridNearMouse()
	{
		for (int x = -gridSize.X / 2; x <= (gridSize.X / 2); x++)
		{
			for (int y = -gridSize.Y / 2; y <= (gridSize.Y / 2); y++)
			{
				currentCell = pixelToCell(GetGlobalMousePosition());
				Vector2I position = (currentCell + new Vector2I(x, y)) * cellSize;
				Rect2 rect = new Rect2(position, cellSize);
				Color mask = defaultCellColor;
				mask.A = mask.A / (1 + 2*(Math.Abs(x) + MathF.Abs(y)));
				DrawRect(rect, mask, false);
			}
		}
	}

	public void drawLineGridNearMouse()
	{
		currentCell = pixelToCell(GetGlobalMousePosition());

		for (int x = -(gridSize.X / 2) + 1; x <= (gridSize.X / 2); x++)
		{
			DrawLine((new Vector2I(x, -gridSize.Y / 2) + currentCell) * cellSize, (new Vector2I(x, 1 +gridSize.Y / 2) + currentCell) * cellSize, defaultLineColor);
		}

		for (int y = (-gridSize.Y / 2) + 1; y <= (gridSize.Y / 2); y++)
		{
			DrawLine((new Vector2I( - gridSize.X / 2, y) + currentCell) * cellSize, (new Vector2I(1 + gridSize.X / 2, y) + currentCell) * cellSize, defaultLineColor);
		}
	}

	public void drawStaticGrid()
	{
		drawOnce = false;
		for (int x = -staticGridSize.X / 2; x <= staticGridSize.X / 2; x++)
		{
			for (int y = (-staticGridSize.Y / 2) ; y <= staticGridSize.Y / 2; y++)
			{
				Vector2I position = new Vector2I(x, y) * cellSize;
				Rect2 rect = new Rect2(position, cellSize);
				Color mask = StaticCellColor;
				mask.A = mask.A / (1 + 2 * (Math.Abs(x) + MathF.Abs(y)));
				DrawRect(rect, mask, false);
			}
		}
	}

	public static Vector2I pixelToCell(Vector2 coords)
	{
		int x = Convert.ToInt32(coords.X) / GenerationSettings.TILE_SIZE;
		int y = Convert.ToInt32(coords.Y) / GenerationSettings.TILE_SIZE;
		return new Vector2I(x, y);
	}
}
