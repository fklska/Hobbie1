@tool
extends TileMapLayer


var coordsset = {}

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("LeftMouseButton"):
		notify_runtime_tile_data_update()
	
	if event.is_action_pressed("RightMouseButton"):
		var tile_data = get_cell_tile_data(local_to_map(get_global_mouse_position()))
		if tile_data:
			print_debug(tile_data.modulate)

func _use_tile_data_runtime_update(coords: Vector2i) -> bool:
	#print_debug(coords == local_to_map(get_global_mouse_position()))
	return coords in coordsset and coords == local_to_map(get_global_mouse_position())
	
func _tile_data_runtime_update(coords: Vector2i, tile_data: TileData) -> void:
	tile_data.modulate.a -= 0.1
	coordsset[coords] = 1
