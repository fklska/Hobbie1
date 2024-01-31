extends Control
class_name SelectorClass

static var selected_object

var prev_selected
@onready var label: Label = $Background/Label
@onready var image: TextureRect = $Background/PanelContainer/TextureRect

func _process(delta):
	if prev_selected != selected_object:
		update_selector()

func update_selector():
	if selected_object != null:
		var data: Dictionary = selected_object.show_selected_info()
		image.texture = data.get("texture")
		label.text = data.get("text", "No data")
		prev_selected = selected_object
