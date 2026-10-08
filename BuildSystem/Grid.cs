using Godot;
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

	public static string PendingBuilding;
	private static Texture2D ghostTexture;
	private static float ghostScale = 1f;
	private bool drawn;

	public WorldScene WorldScene => GameManager.Instance?.World;

	public static void StartPlacement(string buildingId)
	{
		GameManager.BuildingInfo info = GameManager.Buildings[buildingId];
		PendingBuilding = buildingId;
		ghostTexture = GD.Load<Texture2D>(info.GhostPath);
		ghostScale = info.GhostScale;
		buildMode = true;
	}

	public static void StopPlacement()
	{
		PendingBuilding = null;
		ghostTexture = null;
		buildMode = false;
	}

	public override void _Process(double delta)
	{
		if (buildMode)
		{
			if (currentCell != pixelToCell(GetGlobalMousePosition()) || !drawn)
			{
				QueueRedraw();
			}
		}
		else if (drawn)
		{
			QueueRedraw();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!buildMode || @event is not InputEventMouseButton mouse || !mouse.Pressed) return;

		if (mouse.ButtonIndex == MouseButton.Left)
		{
			if (GameManager.Instance.PlaceBuilding(PendingBuilding, pixelToCell(GetGlobalMousePosition()))) StopPlacement();
			GetViewport().SetInputAsHandled();
		}
		else if (mouse.ButtonIndex == MouseButton.Right)
		{
			StopPlacement();
			GetViewport().SetInputAsHandled();
		}
	}

	public HashSet<TileType> BanTilesToPlace = new HashSet<TileType>()
	{
		TileType.DeepWater,
		TileType.TropicWater
	};

	public bool IsAbleToPlace(Vector2I cell)
	{
		Tile tile = WorldScene?.GetTileAt(cell);
		if (tile == null) return false;

		return !BanTilesToPlace.Contains(tile.Type) && tile.Resourse == ResorseType.None;
	}

	public override void _Draw()
	{
		drawn = buildMode;
		if (!buildMode) return;

		drawGridNearMouse();
		drawGhost();
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

	public void drawGhost()
	{
		if (ghostTexture == null) return;

		Vector2I footprint = GameManager.Buildings[PendingBuilding].Footprint;
		Vector2 size = ghostTexture.GetSize() * ghostScale;
		Vector2 center = (currentCell * cellSize) + (footprint * cellSize) / 2;
		bool ok = GameManager.Instance.CanPlace(PendingBuilding, currentCell);
		DrawTextureRect(ghostTexture, new Rect2(center - size / 2, size), false, ok ? new Color(0.6f, 1f, 0.6f, 0.7f) : new Color(1f, 0.4f, 0.4f, 0.7f));
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
