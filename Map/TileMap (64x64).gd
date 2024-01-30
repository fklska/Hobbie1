extends TileMap


func _tile_data_runtime_update(layer, coords, tile_data):
	tile_data.modulate = cells_modulate.get(coords, Color8(255,255,255,255))


func _use_tile_data_runtime_update(layer, coords):
	return cells_modulate.has(coords)
	
var cells_modulate = {}
var mouse_coor
func _process(delta):
	mouse_coor = local_to_map(get_global_mouse_position())
	var cells_coord = get_used_cells(1)
	for coor in cells_coord:
		#print("coord:{0}, player:{1}".format([coor, mouse_coor]))

		if mouse_coor == coor:
			cells_modulate[coor] = Color8(155,155,155,155)
			var tile_data = get_cell_tile_data(1, coor)
			if Input.is_action_just_pressed("LeftMouseButton"):
				var new_data = tile_data.get_custom_data("Data").Health - 10
				tile_data.set_custom_data("Data", new_data)

			if Input.is_action_just_pressed("RightMouseButton"):
				print_debug(tile_data.get_custom_data("Data"))
		else:
			cells_modulate[coor] = Color8(255,255,255,255)

		notify_runtime_tile_data_update(1)
