extends Node2D


const SPEED = 250
var direction = Vector2(1,1)

func _physics_process(delta):
	global_position += SPEED * direction * delta
	print_debug(direction)
