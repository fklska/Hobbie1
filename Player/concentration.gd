extends ProgressBar

@export var player: Player
var conc_value = 0

func _physics_process(delta):
	if Input.is_action_pressed("LeftMouseButton"):
		show()
		value += delta * player.CONCENTRATION
			
	if Input.is_action_just_released("LeftMouseButton"):
		conc_value = value
		value = 0
		hide()
