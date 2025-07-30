@icon("res://Navigation/navigation.png")
extends Node2D
class_name Navigation

@export var chunk_size: int

@export var debug_map_size: Vector2i
@export var rect_color: Color


var polygon_map: Dictionary = {
	#CELL: NAV_REGION
}
var cellSize: Vector2i = Vector2i(512, 512)
const AGENTSIZE = 10
var source_geometry: NavigationMeshSourceGeometryData2D = NavigationMeshSourceGeometryData2D.new()

func _ready() -> void:
	NavigationServer2D.map_set_edge_connection_margin(get_world_2d().navigation_map, 0)
	NavigationServer2D.set_debug_enabled(true)
	
func bake_all_navigation_map(mapSize: Vector2i):
	var map_size = mapSize / 16
	for x in range(map_size.x):
		for y in range(map_size.y + 1):
			bake_navigation_on_cell(Vector2i(x, y))

func bake_navigation_on_agent(agent):
	for x in range(-1, 2):
		for y in range(-1, 2):
			var agent_pos = pixel2cell(agent.global_position)
			bake_navigation_on_cell(Vector2i(x, y) + agent_pos)

func debug_baking():
	for x in range(0, 2):
		for y in range(0, 2):
			bake_navigation_on_cell(Vector2i(x, y))

func debug_draw_grid():
	for x in range(0, debug_map_size.x):
		for y in range(0, debug_map_size.y):
			draw_rect(
				Rect2i(
					Vector2i(x, y) * cellSize,
					cellSize
				),
				rect_color, false
			)

func set_up_navigation_region(navigation_root_node: Node2D):
	var polygon = NavigationPolygon.new()
	var region = NavigationRegion2D.new()
	polygon.source_geometry_mode = NavigationPolygon.SOURCE_GEOMETRY_GROUPS_WITH_CHILDREN
	polygon.source_geometry_group_name = "navigation_polygon_source_geometry_group"
	polygon.agent_radius = AGENTSIZE
	region.navigation_polygon = polygon
	region.use_edge_connections = true
	navigation_root_node.add_child(region)
	return region

func calculate_polygon_coords(cell: Vector2i) -> PackedVector2Array:
	return PackedVector2Array([
		Vector2i(cell.x, cell.y) *cellSize + Vector2i(-AGENTSIZE, -AGENTSIZE), #left top
		Vector2i(cell.x, cell.y + 1)  * cellSize + Vector2i(-AGENTSIZE, AGENTSIZE), # left bot
		Vector2i(cell.x + 1, cell.y + 1)  * cellSize + Vector2i(+AGENTSIZE, +AGENTSIZE), # right bottom
		Vector2i(cell.x + 1, cell.y)  * cellSize + Vector2i(+AGENTSIZE, -AGENTSIZE), # top right
	])

func bake_navigation_on_cell(cell: Vector2i) -> void:
	var region = polygon_hash_map_manager(cell)
	region.bake_navigation_polygon()

func polygon_hash_map_manager(cell: Vector2i):
	if polygon_map.has(cell):
		return polygon_map[cell]
	
	var region = set_up_navigation_region(GlobalNavigation)
	region.navigation_polygon.add_outline(calculate_polygon_coords(cell))
	#print_debug(calculate_polygon_coords(cell), Rect2i(Vector2i(cell.x, (cell.y - 1))  * cellSize,cellSize).grow(2))
	region.navigation_polygon.baking_rect = Rect2i(Vector2i(cell.x, (cell.y))  * cellSize, cellSize).grow(15)
	#region.navigation_polygon.border_size = cellSize.x
	polygon_map[cell] = region
	return region
	
func drawGridAtCell(cell: Vector2i, last_cell: Vector2i):
	if (cell == last_cell): return
	
	var range: int = 2
	for x in range(-range, range):
		var nx = cell.x + x
		for y in range(-range, range):
			var ny = cell.y + y
			draw_rect(
				Rect2i(
					Vector2i(nx, ny) * cellSize,
					cellSize
				),
				rect_color, false
			)

func pixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/chunk_size)
	if sign(pixel.x) == -1:
		x += 1

	var y = int(pixel.y/chunk_size)
	if sign(pixel.y) == -1:
		y -= 1
	return Vector2i(x, y)

func __rebake_map():
	for item in polygon_map:
		polygon_map[item].bake_navigation_polygon()

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("DEBUG"):
		__rebake_map()
