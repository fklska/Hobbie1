@tool
extends NavigationRegion2D
class_name Navigation

@onready var NavigationRegion: NavigationRegion2D = $"."
var chunk_size: int = 512

func _ready() -> void:
	NavigationRegion.navigation_polygon.clear_outlines()
	NavigationRegion.navigation_polygon.clear()
	NavigationRegion.navigation_polygon.add_outline(PackedVector2Array([
			Vector2i(0, 0),
			Vector2i(0, -chunk_size),
			Vector2i(chunk_size, -chunk_size),
			Vector2i(chunk_size, 0)
		]))
	NavigationRegion.bake_navigation_polygon(true)
