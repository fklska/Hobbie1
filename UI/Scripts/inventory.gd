extends CanvasLayer

@onready var anim = $AnimationPlayer

func _input(event):
	if event.is_action_pressed("inventory"):
		if visible == false:
			anim.play("appear")
		else:
			anim.play("disapear")
			await anim.animation_finished
		visible = !visible
