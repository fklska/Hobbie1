@icon("res://Navigation/navigation.png")
extends Node2D
class_name Navigation

@export var chunk_size: int

@export var debug_map_size: Vector2i
@export var rect_color: Color


static var polygon_map: Dictionary = {
	#CELL: NAV_REGION
}
static var cellSize: Vector2i = Vector2i(256, 256)

static var thread: Thread = Thread.new()

var source_geometry: NavigationMeshSourceGeometryData2D = NavigationMeshSourceGeometryData2D.new()


func thread_bake(agent):
	thread.start(non_static_bake_on_agent.bind(agent))

func thread_map_bake():
	thread.start(bake_all_navigation_map)

static func bake_all_navigation_map():
	var map_size = MapGenerator.SIZE
	for x in range(-map_size.x / 8, map_size.x / 8):
		for y in range(-map_size.y / 8, (map_size.y / 8) + 1):
			bake_navigation_on_cell(Vector2i(x, y))

func non_static_bake_on_agent(agent):
	for x in range(-1, 2):
		for y in range(-1, 2):
			var agent_pos = thread.start(pixel2cell.bind(agent.global_position))
			thread.start(non_static_bake_navigation_on_cell.bind(Vector2i(x, y) + agent_pos))

static func bake_navigation_on_agent(agent):
	for x in range(-1, 2):
		for y in range(-1, 2):
			var agent_pos = StaticPixel2cell(agent.global_position)
			bake_navigation_on_cell(Vector2i(x, y) + agent_pos)

func debug_baking():
	for x in range(-1, 2):
		for y in range(-1, 2):
			bake_navigation_on_cell(Vector2i(x, y))

func _ready() -> void:
	queue_redraw()
	NavigationServer2D.map_set_edge_connection_margin(get_world_2d().navigation_map, 0)
	NavigationServer2D.set_debug_enabled(true)
	#thread_map_bake()

func debug_draw_grid():
	for x in range(-debug_map_size.x, debug_map_size.x):
		for y in range(-debug_map_size.y, debug_map_size.y):
			draw_rect(
				Rect2i(
					Vector2i(x, y) * cellSize,
					cellSize
				),
				rect_color, false
			)


static func set_up_navigation_region(navigation_root_node: Node2D):
	var polygon = NavigationPolygon.new()
	var region = NavigationRegion2D.new()
	polygon.source_geometry_mode = NavigationPolygon.SOURCE_GEOMETRY_GROUPS_WITH_CHILDREN
	polygon.source_geometry_group_name = "navigation_polygon_source_geometry_group"
	polygon.agent_radius = 10
	region.navigation_polygon = polygon
	navigation_root_node.call_deferred("add_child", region)
	return region

static func calculate_polygon_coords(cell: Vector2i) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2i(cell.x, cell.y) *cellSize + Vector2i(-15, 15), #left bottom
		Vector2i(cell.x, (cell.y - 1))  * cellSize + Vector2i(-15, -15), # left top
		Vector2i((cell.x + 1), (cell.y - 1))  * cellSize + Vector2i(+15, -15), # top right
		Vector2i((cell.x + 1), cell.y)  * cellSize + Vector2i(+15, +15), # right bottom
	])

static func bake_navigation_on_cell(cell: Vector2i) -> void:
	var region = polygon_hash_map_manager(cell)
	## region.bake_navigation_polygon()

func non_static_bake_navigation_on_cell(cell: Vector2i) -> void:
	var region = thread.start(non_static_polygon_hash_map_manager.bind(cell))

func non_static_polygon_hash_map_manager(cell: Vector2i):
	if polygon_map.has(cell):
		return polygon_map[cell]
	
	var region = set_up_navigation_region(GlobalNavigation)
	region.navigation_polygon.add_outline(calculate_polygon_coords(cell))
	#print_debug(calculate_polygon_coords(cell), Rect2i(Vector2i(cell.x, (cell.y - 1))  * cellSize,cellSize).grow(2))
	region.navigation_polygon.baking_rect = Rect2i(Vector2i(cell.x, (cell.y - 1))  * cellSize,cellSize).grow(cellSize.x)
	region.navigation_polygon.border_size = cellSize.x
	thread.start(region.bake_navigation_polygon)
	polygon_map[cell] = region
	return region 

static func polygon_hash_map_manager(cell: Vector2i):
	if polygon_map.has(cell):
		return polygon_map[cell]
	
	var region = set_up_navigation_region(GlobalNavigation)
	region.navigation_polygon.add_outline(calculate_polygon_coords(cell))
	#print_debug(calculate_polygon_coords(cell), Rect2i(Vector2i(cell.x, (cell.y - 1))  * cellSize,cellSize).grow(2))
	region.navigation_polygon.baking_rect = Rect2i(Vector2i(cell.x, (cell.y - 1))  * cellSize,cellSize).grow(cellSize.x)
	region.navigation_polygon.border_size = cellSize.x
	region.call_deferred("bake_navigation_polygon")
	polygon_map[cell] = region
	return region

static func StaticPixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/cellSize.x)
	if sign(pixel.x) == -1:
		x -= 1

	var y = int(pixel.y/cellSize.y)
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

static func __rebake_map():
	for item in polygon_map:
		polygon_map[item].bake_navigation_polygon()

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("DEBUG"):
		__rebake_map()

func  _draw() -> void:
	debug_draw_grid()
