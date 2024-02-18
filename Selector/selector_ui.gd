extends Control
class_name SelectorInfoClass

@onready var label: Label = $Background/Label
@onready var image: TextureRect = $Background/PanelContainer/TextureRect


func update_selector():
	var selected_object = RectSelectorClass.selected_object
	if selected_object.size() == 1:
		var data: Dictionary = selected_object[0].show_selected_info()
		image.texture = data.get("texture")
		label.text = data.get("text", "No data")
	else:
		#print_debug(selected_object.size())
		image.texture = null
		label.text = "Selected: " + str(selected_object.size()) + " objects"

func _on_panel_draw_end():
	update_selector()
