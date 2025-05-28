extends Node2D


@onready var wood: TileMapLayer = $WOOD
@onready var roo_ck: TileMapLayer = $ROOCk


func _input(event: InputEvent) -> void:
	if event.is_action_pressed("LeftMouseButton"):
		var coords = wood.local_to_map(get_global_mouse_position())
		var tile_data = wood.get_cell_tile_data(coords)
		if tile_data:
			print_debug(tile_data.get_custom_data("HP"))
	
	if event.is_action_pressed("RightMouseButton"):
		var coords = wood.local_to_map(get_global_mouse_position())
		var tile_data = wood.get_cell_tile_data(coords)
		var copy = tile_data.duplicate()
		if tile_data:
			tile_data.set_custom_data("HP", tile_data.get_custom_data("HP") - 10)
			wood._tile_data_runtime_update(coords, tile_data)
			print_debug("Updated -1")
