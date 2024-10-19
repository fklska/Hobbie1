extends InventoryItem
class_name WeaponClass

@export_category("Weapon")
@export var damage: int
@export var required_strench: int

@onready var collisionShape: CollisionShape2D = $CollisionShape2D
var splash_effect = preload("res://effects/splash.tscn")

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
		var direction = global_position.direction_to(get_global_mouse_position())
		new_rotation = direction.angle() + deg_to_rad(amplitude_deegre)
		rotation = new_rotation - deg_to_rad(2*amplitude_deegre)
		var splash = splash_effect.instantiate()
		splash.direction = direction
		splash.global_position = global_position
		splash.rotation = direction.angle()
		get_parent().add_child(splash)
	
	if abs(new_rotation - rotation) < 0.3:
		disable()
		is_ready = true
	else:
		rotation += t*10 / 10

func calculate_damage():
	return parent.calculate_damage() + damage

func _on_body_entered(body):
	if body is ActiveResourses:
		body.get_damage(calculate_damage())
		parent.add_item(body.get_texture(), calculate_damage(), body.type)
