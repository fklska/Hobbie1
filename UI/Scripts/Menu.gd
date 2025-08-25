extends TabContainer
class_name Menu

func close():
	visible = false

func _input(event: InputEvent):
	if event.is_action_pressed("menu"):
		visible = !visible
		BuildMode.buildMode = false


func _on_town_hall_button_down() -> void:
	pass # Replace with function body.


func _on_blacksmith_butt_button_down() -> void:
	pass # Replace with function body.
