extends ItemNode
class_name ItemSword

@onready var collisionShape = $CollisionShape2D

var new_rotation
var amplitude_deegre = 45

func _ready():
	new_rotation = rotation

func _physics_process(delta):
		rotate_sword(delta)
	
func _set_item(value: InventoryItem):
	ITEM = value
	if is_instance_valid(textureRect) and is_instance_valid(collisionShape):
		_update_properties(value)
		
func _update_properties(value):
	if value != null:
		textureRect.texture = ITEM.image
		collisionShape.shape = ITEM.shape
	else:
		textureRect.texture = null
		collisionShape.shape = null
		
func rotate_sword(t):
	if Input.is_action_just_released("LeftMouseButton"):
		show()
		new_rotation = global_position.direction_to(get_global_mouse_position()).angle() + deg_to_rad(amplitude_deegre)
		rotation = new_rotation - deg_to_rad(2*amplitude_deegre)
		#print_debug(rad_to_deg(new_rotation))
		
	rotation = lerp_angle(rotation, new_rotation, t*1)
	
	if abs(new_rotation - rotation) < 0.5:
		hide()

func _on_body_entered(body):
	if body is ActiveResourses:
		body.get_damage(ITEM.damage)
