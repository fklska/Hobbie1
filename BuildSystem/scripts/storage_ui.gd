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
func update_ui(items):
	for item: InventoryItem in items:
		for image: TextureRect in ui_elemnts:
			if image.texture == item.image:
				image.get_child(0).text = str(item.amount)
