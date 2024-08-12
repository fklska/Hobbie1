extends Node2D

@onready var sword = $Sprite2D

var start_rotate = false
var new_rotation

func _ready():
	new_rotation = rotation

func _process(delta):
	if Input.is_action_just_released("LeftMouseButton"):
		new_rotation = self.global_position.direction_to(get_global_mouse_position()).angle()
		rotation = new_rotation - deg_to_rad(45)
		print_debug(rad_to_deg(new_rotation))
		
	rotation = lerp_angle(rotation, new_rotation + deg_to_rad(45), delta*2)
