extends Node2D


@onready var wood: TileMapLayer = $WOOD
@onready var roo_ck: TileMapLayer = $ROOCk
@export var steps: Array = []

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("LeftMouseButton"):
		var coords = wood.local_to_map(get_global_mouse_position())
		var tile_data = wood.get_cell_tile_data(coords)
		if tile_data:
			print_debug(tile_data.get_custom_data("HP"))
	
	if event.is_action_pressed("RightMouseButton"):
		wood.notify_runtime_tile_data_update()
