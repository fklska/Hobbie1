extends Control
class_name SelectorInfoClass

@onready var label: Label = $Background/Label
@onready var image: TextureRect = $Background/PanelContainer/TextureRect
#@onready var buttons_container: HBoxContainer = $Background/HBoxContainer

func update_selector():
	#clear_buttons()

	var selected_object = RectSelectorClass.selected_object
	if selected_object.size() == 1:
		var data: Dictionary = selected_object[0].show_selected_info()
		image.texture = data.get("texture")
		label.text = data.get("text", "No data")
		#var actions = data.get("action")
		#if actions:
			#var index = 0
			#var buttons = buttons_container.get_children()
			#for i: Callable in actions:
				#buttons[index].pressed.connect(i)
				#index += 1
	else:
		#print_debug(selected_object.size())
		image.texture = null
		label.text = "Selected: " + str(selected_object.size()) + " objects"

#func clear_buttons():
	#var buttons = buttons_container.get_children()
	#for butt: Button in buttons:
		#var connections = butt.pressed.get_connections()
		##print_debug(connections)
		#if !connections.is_empty():
			#butt.pressed.disconnect(connections[0].get("callable"))

func _on_rect_seelctor_draw_end():
	update_selector()
