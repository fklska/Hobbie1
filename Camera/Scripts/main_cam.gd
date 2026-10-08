extends Camera2D

const ZOOM_STEP := 0.1
const ZOOM_MIN := 0.1
const ZOOM_MAX := 3.0


func _input(event):
	if event.is_action_pressed("zoom+"):
		zoom = Vector2.ONE * clampf(zoom.x + ZOOM_STEP, ZOOM_MIN, ZOOM_MAX)
		
	if event.is_action_pressed("zoom-"):
		zoom = Vector2.ONE * clampf(zoom.x - ZOOM_STEP, ZOOM_MIN, ZOOM_MAX)
