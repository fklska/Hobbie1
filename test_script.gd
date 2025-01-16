@tool
extends NavigationRegion2D

@onready var NavigationRegion: NavigationRegion2D = $"."

@export var basis_vector_hor: Vector2i
@export var basis_vector_ver: Vector2i
@export var chunk_size: int

@export var debug_map_size: Vector2i
@export var rect_color: Color

func _ready() -> void:
	pass
	
func debug_draw_grid():
	for x in range(-debug_map_size.x, debug_map_size.x):
		for y in range(-debug_map_size.y, debug_map_size.y):
			draw_rect(
				Rect2i(
					Vector2i(x * chunk_size, y*chunk_size),
					Vector2i(chunk_size, chunk_size)
				),
				rect_color, false
			)

func calculate_polygon_coords(cell: Vector2i) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2i(cell.x * chunk_size, cell.y * chunk_size), #left bottom
		Vector2i(cell.x * chunk_size, (cell.y - 1) * chunk_size), # left top
		Vector2i((cell.x + 1) * chunk_size, (cell.y - 1) * chunk_size), # top right
		Vector2i((cell.x + 1) * chunk_size, cell.y * chunk_size), # right bottom
	])

func bake_navigation_on_cell(coord: PackedVector2Array) -> void:
	NavigationRegion.navigation_polygon.add_outline(coord)
	NavigationRegion.bake_navigation_polygon(true)

func pixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/chunk_size)
	if sign(pixel.x) == -1:
		x -= 1

	var y = int(pixel.y/chunk_size)
	if sign(pixel.y) == 1:
		y += 1
	return Vector2i(x, y)

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("LeftMouseButton"):
		print_debug(pixel2cell(get_global_mouse_position()), get_global_mouse_position())
		bake_navigation_on_cell(calculate_polygon_coords(pixel2cell(get_global_mouse_position())))

func  _draw() -> void:
	debug_draw_grid()
