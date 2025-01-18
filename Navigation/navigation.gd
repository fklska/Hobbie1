@tool
extends Node2D
class_name Navigation

@export var basis_vector_hor: Vector2i
@export var basis_vector_ver: Vector2i
@export var chunk_size: int

@export var debug_map_size: Vector2i
@export var rect_color: Color

var source_geometry: NavigationMeshSourceGeometryData2D = NavigationMeshSourceGeometryData2D.new()

func _ready() -> void:
	queue_redraw()
	bake_navigation_on_cell(set_up_navigation_region(self), calculate_polygon_coords(Vector2i(0,-1)))
	NavigationServer2D.map_set_edge_connection_margin(get_world_2d().navigation_map, 100)
	
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

func set_up_navigation_region(navigation_root_node: Node2D):
	var polygon = NavigationPolygon.new()
	var region = NavigationRegion2D.new()
	polygon.source_geometry_mode = NavigationPolygon.SOURCE_GEOMETRY_GROUPS_WITH_CHILDREN
	polygon.source_geometry_group_name = "navigation_polygon_source_geometry_group"
	region.navigation_polygon = polygon
	navigation_root_node.add_child(region)
	return region

func calculate_polygon_coords(cell: Vector2i) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2i(cell.x * chunk_size, cell.y * chunk_size), #left bottom
		Vector2i(cell.x * chunk_size, (cell.y - 1) * chunk_size), # left top
		Vector2i((cell.x + 1) * chunk_size, (cell.y - 1) * chunk_size), # top right
		Vector2i((cell.x + 1) * chunk_size, cell.y * chunk_size), # right bottom
	])

func bake_navigation_on_cell(navigation_region: NavigationRegion2D, coord: PackedVector2Array) -> void:
	navigation_region.navigation_polygon.add_outline(coord)
	navigation_region.bake_navigation_polygon()
	#NavigationServer2D.bake_from_source_geometry_data(navigation_region.navigation_polygon, )

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
		bake_navigation_on_cell(set_up_navigation_region(self), calculate_polygon_coords(pixel2cell(get_global_mouse_position())))

func  _draw() -> void:
	debug_draw_grid()
