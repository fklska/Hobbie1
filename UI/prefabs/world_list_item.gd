extends MarginContainer
class_name WorldListItem

@export var WorldPreview: TextureRect
@export var WorldName: Label
@export var WorldData: SimpleGeneratorData
@export var WorldSeed: SpinBox
@export var WorldDataPath: String

@onready var player = load("res://Player/Player.tscn").instantiate()

func _on_load_world_button_down() -> void:
	var world = load(WorldDataPath.get_basename() + ".tscn").instantiate()
	get_tree().root.add_child(world)
	player.position = WorldData.SpawnPoint
	get_tree().root.add_child(player)
	get_tree().root.get_node("World/UI/Menu").visible = false
	#Navigation.bake_all_navigation_map(WorldData.mapSize)
