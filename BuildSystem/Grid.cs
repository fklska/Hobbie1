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
	public static int PendingTurn;
	private static Texture2D ghostTexture;
	private static Rect2 ghostRect;
	private static bool rotateHinted;
	private static readonly Dictionary<string, int> lastTurn = new();
	private bool drawn;

	public WorldScene WorldScene => GameManager.Instance?.World;

	public static void StartPlacement(string buildingId)
	{
		PendingBuilding = buildingId;
		PendingTurn = Economy.Buildings[buildingId].Fixed ? 0 : lastTurn.GetValueOrDefault(buildingId);
		UpdateGhost();
		buildMode = true;
		if (rotateHinted || Economy.Buildings[buildingId].Fixed) return;
		rotateHinted = true;
		GameManager.Instance.Notify("R или колесо мыши поворачивают здание, ПКМ отменяет");
	}

	private static void UpdateGhost()
	{
		Node building = GD.Load<PackedScene>(Economy.Buildings[PendingBuilding].ScenePath).Instantiate();
		Godot.Collections.Dictionary preview = building.Call("preview", GameManager.Instance.GetLevel(PendingBuilding), PendingTurn).AsGodotDictionary();
		building.Free();
		ghostTexture = preview.Count > 0 ? preview["texture"].As<Texture2D>() : null;
		ghostRect = preview.Count > 0 ? preview["rect"].AsRect2() : default;
	}

	private void Rotate(int step)
	{
		if (Economy.Buildings[PendingBuilding].Fixed) return;
		PendingTurn = Mathf.PosMod(PendingTurn + step, 4);
		lastTurn[PendingBuilding] = PendingTurn;
		UpdateGhost();
		QueueRedraw();
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
		if (!buildMode) return;
		if (@event.IsActionPressed("rotate_building", false, true))
		{
			Rotate(1);
			GetViewport().SetInputAsHandled();
			return;
		}
		if (@event is not InputEventMouseButton mouse || !mouse.Pressed) return;

		if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
		{
			Rotate(mouse.ButtonIndex == MouseButton.WheelUp ? 1 : -1);
			GetViewport().SetInputAsHandled();
		}
		else if (mouse.ButtonIndex == MouseButton.Left)
		{
			if (GameManager.Instance.PlaceBuildingAt(PendingBuilding, pixelToCell(GetGlobalMousePosition()), PendingTurn)) StopPlacement();
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
		WorldScene world = WorldScene;
		if (!IsInstanceValid(world) || world.Map == null || !world.Map.InBounds(cell)) return false;

		return !BanTilesToPlace.Contains(world.GetBiomeAt(cell)) && world.GetResourceAt(cell) == ResorseType.None;
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

		Vector2I footprint = Economy.Footprint(PendingBuilding, PendingTurn);
		Vector2 origin = currentCell * cellSize;
		bool ok = GameManager.Instance.CanPlaceAt(PendingBuilding, currentCell, PendingTurn);
		Color tint = ok ? new Color(0.6f, 1f, 0.6f, 0.7f) : new Color(1f, 0.4f, 0.4f, 0.7f);
		DrawRect(new Rect2(origin, footprint * cellSize), tint with { A = 0.25f });
		DrawTextureRect(ghostTexture, new Rect2(origin + ghostRect.Position, ghostRect.Size), false, tint);
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
