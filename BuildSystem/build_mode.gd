extends Node2D
class_name BuildModeManager

var build_mode: bool = false:
	set(value):
		build_mode = value
		queue_redraw()

@export_category("BuildManager")
@export var Rectangle: Rect2
@export var RectColor: Color
@export var SelectRectColor: Color
@export_category("Grid")
@export var basis_vec_hor: Vector2i
@export var basis_vec_ver: Vector2i
@export var cell_size: Vector2i
@export var map_size: Vector2i

var last_selected_cell = null
var size_multiplier: int = 1:
	set(value):
		size_multiplier = value
		queue_redraw()

func _process(delta):
	need_to_redraw_selected_cell()
	
func need_to_redraw_selected_cell():
	if pixel2cell(get_global_mouse_position()) != last_selected_cell and last_selected_cell != null:
		queue_redraw()

func draw_grid():
	for x in range(-map_size.x, map_size.x):
		for y in range(-map_size.y, map_size.y):
			draw_rect(Rect2i(x*basis_vec_hor + y*basis_vec_ver,cell_size), RectColor, false)

func draw_selected_cell(size_multiplier: int = 1):
	var coor: Vector2i = pixel2cell(get_global_mouse_position())
	if size_multiplier > 1:
		coor = coor - Vector2i(1, 1)
	draw_rect(Rect2i(coor.x*basis_vec_hor + coor.y*basis_vec_ver, cell_size * size_multiplier), SelectRectColor, false)
	last_selected_cell = coor

func pixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/cell_size.x)
	var y = int(pixel.y/cell_size.y)
	return Vector2i(x, y)

func cell2pixel(cell: Vector2) -> Vector2i:
	return Vector2i(cell.x * cell_size.x, cell.y*cell_size.y)

func _draw():
	if build_mode:
		draw_grid()
		draw_selected_cell(size_multiplier)
