@tool
extends Node2D

@onready var navigation_region_2d: NavigationRegion2D = $NavigationRegion2D
@onready var static_body_2d: StaticBody2D = $StaticBody2D

var path_start_position: Vector2

@export_range(-100, 100) var grow_ind = 0

func _ready() -> void:
	queue_redraw()
	var source_geometry: NavigationMeshSourceGeometryData2D = NavigationMeshSourceGeometryData2D.new()
	var traversable_outline: PackedVector2Array = PackedVector2Array([
		Vector2(0.0, 0.0),
		Vector2(1920.0, 0.0),
		Vector2(1920.0, 1080.0),
		Vector2(0.0, 1080.0),
	])
	source_geometry.add_traversable_outline(traversable_outline)
	NavigationServer2D.parse_source_geometry_data(navigation_region_2d.navigation_polygon, source_geometry, static_body_2d)
	NavigationServer2D.bake_from_source_geometry_data(navigation_region_2d.navigation_polygon, source_geometry)


func _draw() -> void:
	draw_rect(Rect2i(0, 0, 64, 64).grow(grow_ind), Color.RED)
