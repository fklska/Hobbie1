extends InventoryItem
class_name WeaponClass

@export_category("Weapon")
@export var damage: int
@export var required_strench: int

@onready var collisionShape: CollisionShape2D = $CollisionShape2D

var new_rotation
var amplitude_deegre = 45
var is_ready = true
var parent: Player = null

func _ready():
	new_rotation = rotation

func _physics_process(delta):
	rotate_sword(delta)

func set_selected(args: Dictionary):
	parent = args.get("Parent")
	parent.add_child(self)

func disable():
	hide()
	collisionShape.disabled = true

func enable():
	show()
	collisionShape.disabled = false
	is_ready = false

func rotate_sword(t):
	if Input.is_action_just_released("LeftMouseButton") and is_ready:
		enable()
		new_rotation = global_position.direction_to(get_global_mouse_position()).angle() + deg_to_rad(amplitude_deegre)
		rotation = new_rotation - deg_to_rad(2*amplitude_deegre)

	rotation = lerp_angle(rotation, new_rotation, t*parent.AGILITY / 10)
	
	if abs(new_rotation - rotation) < 0.3:
		disable()
		is_ready = true

func calculate_damage():
	return parent.calculate_damage() + damage

func _on_body_entered(body):
	if body is ActiveResourses:
		body.get_damage(calculate_damage())
		parent.add_item(body.get_texture(), calculate_damage(), body.type)
