extends Control
class_name StorageUI

var status: bool = false

@onready var anim: AnimationPlayer = $"../AnimationPlayer"

@onready var ui_elemnts = $HBoxContainer.get_children()
func update_ui(data: Dictionary):
	for image: TextureRect in ui_elemnts:
		image.get_child(0).text = str(data[image.texture])
