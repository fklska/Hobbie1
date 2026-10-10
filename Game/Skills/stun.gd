extends Node2D
class_name Stun

const FRAMES := preload("res://Art/effects/spell_frames.tres")
const TIMERS := ["attack_timer", "melee_timer", "laser_timer"]

var time_left := 0.0
var _target: Node2D
var _speed := 0.0


static func apply(target: Node2D, time: float) -> void:
	if not is_instance_valid(target) or target.get("hp") <= 0 or not "speed" in target:
		return
	var stun: Stun = target.get_node_or_null(^"Stun")
	if stun == null:
		stun = Stun.new()
		stun.name = "Stun"
		stun.time_left = time
		target.add_child(stun)
	else:
		stun.time_left = maxf(stun.time_left, time)


func _ready() -> void:
	_target = get_parent()
	_speed = _target.speed
	z_index = 2
	var radius = _target.get("hit_radius")
	var sprite := AnimatedSprite2D.new()
	sprite.sprite_frames = FRAMES
	sprite.play(&"stun")
	sprite.position = Vector2(0, -2.0 * (radius if radius != null else 14.0) - 8.0)
	add_child(sprite)


func _physics_process(delta: float) -> void:
	time_left -= delta
	if is_instance_valid(_target):
		_target.speed = 0.0
		for timer in TIMERS:
			if timer in _target:
				_target.set(timer, maxf(_target.get(timer), time_left))
	if time_left <= 0.0:
		queue_free()


func _exit_tree() -> void:
	if is_instance_valid(_target):
		_target.speed = _speed
