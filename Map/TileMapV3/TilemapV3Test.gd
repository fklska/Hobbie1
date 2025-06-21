extends Node2D

@onready var tile_map_layer: TileMapLayer = $TileMapLayer


func _ready() -> void:
	gen()
	

func gen():
	tile_map_layer.clear()

	for x in range(64):
		for y in range(64):
			var rand = 0
			if x % 2 == 0 and y % 2 == 0:
				rand = 1
				tile_map_layer.set_cell(Vector2i(x, y), 0, Vector2i(2, 1), 0)
