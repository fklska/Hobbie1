extends CanvasLayer


@onready var anim = $AnimationPlayer

func _input(event: InputEvent):
	if event.is_action_pressed("inventory"):
		print_debug("Pressed")
		if visible == false:		
			anim.play("appear")
		else:
			anim.play("disapear")
			await anim.animation_finished
		visible = !visible
