extends StaticBody2D


@export var HEALTH = 100
@export var STORAGE = 100
# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass


func _on_mouse_entered():
	modulate = Color8(155, 155, 155) # Replace with function body.


func _on_mouse_exited():
	modulate = Color8(255, 255, 255, 255) # Replace with function body.
