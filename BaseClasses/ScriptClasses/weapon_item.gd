extends InventoryItem
class_name WeaponClass

@export_category("Weapon")
@export var damage: int
@export var required_strench: int

@onready var collisionShape: CollisionShape2D = $CollisionShape2D
@onready var effectLocator: Marker2D = $EffectLocator
var splash_effect = preload("res://effects/splash.tscn")

var new_rotation 
var amplitude_deegre = 45
var is_ready = true
var parent: Player = null
var elapsed: float = 0.0
var splash
var splashSpawned: bool = false

func _ready():
	new_rotation = rotation

func _physics_process(delta):
	DoFunction(delta)

func DoFunction(delta):
	if IsRotateComplete():
		disable()
	else:
		rotate_sword(delta)

func IsRotateComplete():
	if abs(rotation - new_rotation) < 0.1:
		return true
	else:
		return false

func IsRotatePassPoint(point: float):
	return abs(rotation - new_rotation) < point

func disable():
	hide()
	collisionShape.disabled = true
	is_ready = true
	splashSpawned = false

func enable():
	show()
	collisionShape.disabled = false
	is_ready = false

func SetRotationDirection():
	elapsed = 0.0
	enable()
	var direction = global_position.direction_to(get_global_mouse_position())
	new_rotation = direction.angle() + deg_to_rad(amplitude_deegre)
	rotation = new_rotation - deg_to_rad(2*amplitude_deegre)
	return direction

func PreSpawnSwordSplash(direction):
	splash = splash_effect.instantiate()
	splash.direction = direction
	splash.rotation = direction.angle()

func SpawnSplash():
	if !IsSplashSpawned():
		splash.global_position = effectLocator.global_position
		splashSpawned = true
		get_parent().add_child(splash)

func IsSplashSpawned():
	return splashSpawned

func rotate_sword(t):

	rotation = lerp_angle(rotation, new_rotation, elapsed * parametrs.get("Player_Agility", 1)/ 10)
	elapsed += t
	
	if IsRotatePassPoint(1.5):
		SpawnSplash()

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton") and is_ready:
		PreSpawnSwordSplash(SetRotationDirection())

func calculate_damage():
	return damage

func _on_body_entered(body):
	if body is ActiveResourses:
		body.get_damage(calculate_damage())
		parent.add_item(body.get_texture(), calculate_damage(), body.type)
