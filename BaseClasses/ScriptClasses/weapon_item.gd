extends InventoryItem
class_name WeaponClass

@export_category("Weapon")
@export var damage: int
@export var required_strench: int
@export_range(1, 600) var attack_speed: int

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

var tween = null

func _ready():
	new_rotation = rotation
	disable()

func DoFunction(params={}):
	tween = create_tween() as Tween
	enable()
	tween.tween_property(self, "rotation", SetRotationDirection(), calculate_attack_speed())
	print_debug(calculate_attack_speed())
	await tween.finished
	disable()

func calculate_attack_speed():
	return float(15*(parametrs.get("Agility", 10)+ attack_speed + 10)) / (parametrs.get("Agility", 10)*(attack_speed + 10))
		

func IsRotateComplete():
	if abs(rotation - new_rotation) < 0.1:
		return true
	return false

func IsRotatePassPoint(point: float):
	return abs(rotation - new_rotation) < point

func disable():
	hide()
	collisionShape.disabled = true
	is_ready = true
	splashSpawned = false
	if tween:
		tween.kill()

func enable():
	show()
	collisionShape.disabled = false
	is_ready = false

func SetRotationDirection():
	enable()
	var direction = global_position.direction_to(get_global_mouse_position())
	new_rotation = direction.angle() + deg_to_rad(amplitude_deegre)
	rotation = new_rotation - deg_to_rad(2*amplitude_deegre)
	return new_rotation

func PreSpawnSwordSplash(direction):
	splash = splash_effect.instantiate()
	splash.direction = direction
	splash.rotation = direction.angle()

func SpawnSplash():
	if !IsSplashSpawned():
		splash.global_position = effectLocator.global_position
		splashSpawned = true
		get_tree().root.add_child(splash)

func IsSplashSpawned():
	return splashSpawned

func rotate_sword(t):

	rotation = lerp_angle(rotation, new_rotation, elapsed * parametrs.get("Player_Agility", 1)/ 10)
	elapsed += t
	
	if IsRotatePassPoint(1.5):
		SpawnSplash()

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton") and is_ready:
		DoFunction()
	
	# DEBUG
	if event.is_action_pressed("RightMouseButton"):
		damage += 1
	#END DEBUG
	
	if event.is_action("Movement"):
		disable()

func calculate_damage():
	return damage

func _on_body_entered(body):
	if body is ActiveResourses:
		if is_instance_valid(body):
			body.get_damage(calculate_damage())
		if is_instance_valid(parent):
			parent.add_item(body.get_texture(), calculate_damage(), body.type)
