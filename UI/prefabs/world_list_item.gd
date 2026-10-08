extends MarginContainer
class_name WorldListItem

@export var WorldPreview: TextureRect
@export var WorldName: Label
@export var WorldData: SimpleGeneratorData
@export var WorldSeed: SpinBox
@export var WorldDataPath: String

func _on_load_world_button_down() -> void:
	var scene_path := WorldDataPath.get_basename() + ".tscn"
	if Game.HasRun(scene_path):
		Game.ContinueRun(scene_path)
	else:
		Game.StartWorld(scene_path, WorldData.SpawnPoint)
