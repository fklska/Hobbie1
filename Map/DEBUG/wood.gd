extends TileMapLayer

func _use_tile_data_runtime_update(coords: Vector2i) -> bool:
	return coords == local_to_map(get_global_mouse_position())

func _tile_data_runtime_update(coords: Vector2i, tile_data: TileData) -> void:
	#tile_data.modulate.a = 0.3
	print_debug(tile_data.get_custom_data("HP") - 10)
	tile_data.set_custom_data("HP", tile_data.get_custom_data("HP") - 10)
