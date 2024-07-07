extends TabContainer
class_name Menu

func close():
	visible = false

func _input(event: InputEvent):
	if event.is_action_pressed("menu"):
		visible = !visible
