extends MarginContainer
class_name WorldListItem

@export var WorldPreview: TextureRect
@export var WorldName: Label
@export var WorldSeed: SpinBox
@export var WorldDataPath: String

func _on_load_world_button_down() -> void:
	print_debug(WorldDataPath.get_basename())
