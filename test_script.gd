extends Node2D

@onready var progressBar: ProgressBar = $ProgressBar
var value = 0

func _physics_process(delta):
	if Input.is_action_pressed("LeftMouseButton"):
			value += delta * 10
			progressBar.value = value
			
	if Input.is_action_just_released("LeftMouseButton"):
		value = 0
		progressBar.value = 0
