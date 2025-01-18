extends Node2D

@onready var debug_path: Node2D = $Node2D
@onready var agent: NavigationAgent2D = $Node2D/NavigationAgent2D

var path_start_position: Vector2


func _process(_delta: float) -> void:
	var mouse_cursor_position: Vector2 = get_global_mouse_position()

	var map: RID = get_world_2d().navigation_map
	# Do not query when the map has never synchronized and is empty.
	if NavigationServer2D.map_get_iteration_id(map) == 0:
		return

	var closest_point_on_navmesh: Vector2 = NavigationServer2D.map_get_closest_point(
		map,
		mouse_cursor_position
	)

	if Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE):
		path_start_position = closest_point_on_navmesh

	debug_path.global_position = path_start_position

	agent.target_position = closest_point_on_navmesh
	#%PathDebugEdgeCentered.target_position = closest_point_on_navmesh

	agent.get_next_path_position()
	#%PathDebugEdgeCentered.get_next_path_position()
