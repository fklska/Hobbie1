extends TabContainer


func _input(event: InputEvent):
	if event.is_action_pressed("menu"):
		visible = !visible
