@tool
extends Node2D

@onready var tile_map_dual_v_3: TileMapLayer = $TileMapDualV3

func _ready() -> void:
	gen()
	

func gen():
	tile_map_dual_v_3.clear()
	
	for x in range(64):
		for y in range(64):
			var rand = 0
			if x % 2 == 0 and y % 2 == 0:
				rand = 1
			tile_map_dual_v_3.set_cell(Vector2(x, y), 0, Vector2(2,1), rand)
