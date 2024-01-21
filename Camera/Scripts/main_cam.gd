extends Camera2D


func _input(event):
	if event.is_action_pressed("zoom+"):
		zoom = Vector2(zoom.x + 0.1, zoom.y + 0.1)
		
	if event.is_action_pressed("zoom-"):
		zoom = Vector2(zoom.x - 0.1, zoom.y - 0.1)
