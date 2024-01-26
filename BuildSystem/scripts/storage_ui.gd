extends Control
class_name StorageUI

var status: bool = false

@onready var anim: AnimationPlayer = $"../AnimationPlayer"

func _on_texture_button_pressed():
	status = !status
	if status:
		anim.play("show")
	else:
		anim.play("hide")

@onready var ui_elemnts = $TextureButton/HBoxContainer.get_children()
func update_ui(data: Array[InventoryItem]):
	for image: TextureRect in ui_elemnts:
		for i in data:
			if image.texture == i.image:
				image.get_child(0).text = str(i.amount)
