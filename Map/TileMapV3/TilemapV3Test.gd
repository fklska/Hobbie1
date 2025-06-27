@tool
extends Node2D

@export var value: float

@onready var tile_map_dual: TileMapDual = $TileMapDual

func _ready() -> void:
	#gen()
	pass
	

func gen():
	tile_map_dual.clear()

	for x in range(64):
		for y in range(64):
			var rand = 0
			if x % 2 == 0 and y % 2 == 0:
				rand = 1
				tile_map_dual.set_cell(Vector2i(x, y), 0, Vector2i(2, 1), 0)


func _on_property_list_changed() -> void:
	print_debug("Changed")
