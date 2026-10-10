extends Node2D

const SPEED := 760.0
const FX := preload("res://Game/Skills/spell_fx.tscn")

@onready var sprite: AnimatedSprite2D = $Sprite
@onready var light: PointLight2D = $Light

var _target := Vector2.ZERO
var _damage := 1
var _radius := 80.0
var _burn_dps := 0.0
var _burn_time := 0.0
var _pyre := 0.0
var _follow: Node2D


func launch(from: Vector2, to: Vector2, damage: int, radius: float, burn_dps: float, burn_time: float, pyre: float, follow: Node2D = null) -> void:
	global_position = from
	_target = to
	_damage = damage
	_radius = radius
	_burn_dps = burn_dps
	_burn_time = burn_time
	_pyre = pyre
	_follow = follow
	rotation = (to - from).angle() - PI / 2.0


func _physics_process(delta: float) -> void:
	if Burning.alive(_follow):
		_target = _follow.global_position
		rotation = (_target - global_position).angle() - PI / 2.0
	var step := SPEED * delta
	if global_position.distance_to(_target) <= step:
		global_position = _target
		_burst()
		return
	global_position = global_position.move_toward(_target, step)


func _burst() -> void:
	set_physics_process(false)
	var fx := FX.instantiate()
	get_parent().add_child(fx)
	fx.global_position = _target
	fx.scale = Vector2.ONE * 1.2
	fx.call("play_fx", &"fire_burst")
	Sound.PlayAt("burn", _target)
	for node in get_tree().get_nodes_in_group("enemies"):
		if not Burning.alive(node):
			continue
		var reach: float = _radius + (node.get("hit_radius") if node.get("hit_radius") != null else 16.0)
		if node.global_position.distance_to(_target) <= reach:
			Game.Damage(node, _damage)
			Burning.apply(node, _burn_dps, _burn_time, _pyre)
	sprite.hide()
	var tween := create_tween()
	tween.tween_property(light, "energy", 0.0, 0.35).from(2.2)
	tween.tween_callback(queue_free)
