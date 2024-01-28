extends Control
class_name SelectorClass

static var selected_object

var prev_selected
@onready var label: Label = $Background/Label
@onready var image: TextureRect = $Background/TextureRect

func _process(delta):
	if prev_selected != selected_object:
		update_selector()

func update_selector():
	if selected_object != null:
		var data = selected_object.show_selected_info()
		image.texture = data[0]
		label.text = data[1]
		prev_selected = selected_object
