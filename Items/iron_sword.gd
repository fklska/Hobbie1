extends ItemNode
class_name ItemSword

@onready var collisionShape = $CollisionShape2D

var new_rotation
const amplitude_deegre = 45

func _ready():
	new_rotation = rotation

func _process(delta):
	rotate_sword(delta)

func rotate_sword(delta):
	if Input.is_action_just_released("LeftMouseButton"):
		new_rotation = global_position.direction_to(get_global_mouse_position()).angle()
		rotation = new_rotation - deg_to_rad(amplitude_deegre)
		print_debug(rad_to_deg(new_rotation))
		
	rotation = lerp_angle(rotation, new_rotation + deg_to_rad(45), delta*2)

func _set_item(value: InventoryItem):
	ITEM = value
