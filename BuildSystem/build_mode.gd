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

var last_selected_cell = Vector2i(0, 0)

func _process(delta):
	need_to_redraw_selected_cell()
	
func need_to_redraw_selected_cell():
	if pixel2cell(get_global_mouse_position()) != last_selected_cell:
		queue_redraw()

func draw_grid():
	for x in map_size.x:
		for y in map_size.y:
				draw_rect(Rect2i(x*basis_vec_hor + y*basis_vec_ver,cell_size), RectColor, false)

func draw_selected_cell():
	var coor: Vector2i = pixel2cell(get_global_mouse_position())
	draw_rect(Rect2i(coor.x*basis_vec_hor + coor.y*basis_vec_ver,cell_size), SelectRectColor, false)

func pixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/cell_size.x)
	var y = int(pixel.y/cell_size.y)
	return Vector2i(x, y)

func _draw():
	if build_mode:
		draw_grid()
		draw_selected_cell()
