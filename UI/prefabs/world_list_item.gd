extends MarginContainer
class_name WorldListItem

@export var WorldPreview: TextureRect
@export var WorldName: Label
@export var WorldData: SimpleGeneratorData
@export var WorldSeed: SpinBox
@export var WorldDataPath: String

func _on_load_world_button_down() -> void:
	Game.StartWorld(WorldDataPath.get_basename() + ".tscn", WorldData.SpawnPoint)
