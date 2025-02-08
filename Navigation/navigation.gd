@tool
extends Node2D
class_name Navigation

@export var basis_vector_hor: Vector2i
@export var basis_vector_ver: Vector2i
@export var chunk_size: int

@export var debug_map_size: Vector2i
@export var rect_color: Color


static var polygon_map: Dictionary = {
	#CELL: NAV_REGION
}
static var cellSize = 256


var source_geometry: NavigationMeshSourceGeometryData2D = NavigationMeshSourceGeometryData2D.new()

static func bake_navigation_on_agent(agent):
	for x in range(-1, 2):
		for y in range(-1, 2):
			var agent_pos = StaticPixel2cell(agent.global_position)
			bake_navigation_on_cell(Vector2i(x, y) + agent_pos, cellSize)

func _ready() -> void:
	queue_redraw()
	#bake_navigation_on_cell(set_up_navigation_region(self), calculate_polygon_coords(Vector2i(0,-1)))
	NavigationServer2D.map_set_edge_connection_margin(get_world_2d().navigation_map, 256)
	
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


static func set_up_navigation_region(navigation_root_node: Node2D):
	var polygon = NavigationPolygon.new()
	var region = NavigationRegion2D.new()
	polygon.source_geometry_mode = NavigationPolygon.SOURCE_GEOMETRY_GROUPS_WITH_CHILDREN
	polygon.source_geometry_group_name = "navigation_polygon_source_geometry_group"
	region.navigation_polygon = polygon
	navigation_root_node.add_child(region)
	return region

static func calculate_polygon_coords(cell: Vector2i) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2i(cell.x * cellSize, cell.y * cellSize), #left bottom
		Vector2i(cell.x * cellSize, (cell.y - 1) * cellSize), # left top
		Vector2i((cell.x + 1) * cellSize, (cell.y - 1) * cellSize), # top right
		Vector2i((cell.x + 1) * cellSize, cell.y * cellSize), # right bottom
	])

static func bake_navigation_on_cell(cell: Vector2i, cell_size) -> void:
	var region = polygon_hash_map_manager(cell)
	region.navigation_polygon.add_outline(calculate_polygon_coords(cell))
	region.bake_navigation_polygon()

static func polygon_hash_map_manager(cell: Vector2i):
	if polygon_map.has(cell):
		return polygon_map[cell]
	
	var region = set_up_navigation_region(GlobalNavigation)
	polygon_map[cell] = region
	return region

static func StaticPixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/cellSize)
	if sign(pixel.x) == -1:
		x -= 1

	var y = int(pixel.y/cellSize)
	if sign(pixel.y) == 1:
		y += 1
	return Vector2i(x, y)

func pixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/chunk_size)
	if sign(pixel.x) == -1:
		x -= 1

	var y = int(pixel.y/chunk_size)
	if sign(pixel.y) == 1:
		y += 1
	return Vector2i(x, y)

func _input(event: InputEvent) -> void:
	pass

func  _draw() -> void:
	#debug_draw_grid()
	pass
