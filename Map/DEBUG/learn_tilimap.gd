@tool
extends Node2D


@onready var land: TileMapDual = $Land

func _ready() -> void:
	land.clear()
	
	var land_coords = []
	var sand_coords = []
	for x in range(0, 64):
		for y in range(0, 64):
			if x % 2 == 0 and y % 2 == 0:
				land_coords.append(Vector2i(x, y))
				#land.set_cell(Vector2i(x, y), 0, Vector2i(2, 1), 0)
				
	land.set_cells_terrain_connect(land_coords, 0, 0, false)
