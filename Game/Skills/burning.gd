extends Node2D
class_name Burning

const TICK := 0.5
const FRAMES := preload("res://Art/effects/spell_frames.tres")
const UNSHADED := preload("res://Game/Skills/unshaded.tres")
const FX := preload("res://Game/Skills/spell_fx.tscn")

var dps := 0.0
var duration := 0.0
var time_left := 0.0
var pyre := 0.0
var _tick := TICK
var _target: Node2D


static func apply(target: Node2D, burn_dps: float, time: float, pyre_radius: float) -> void:
	if not alive(target):
		return
	var burning: Burning = target.get_node_or_null(^"Burning")
	if burning == null:
		burning = Burning.new()
		burning.name = "Burning"
		target.add_child(burning)
	burning.dps = maxf(burning.dps, burn_dps)
	burning.duration = maxf(burning.duration, time)
	burning.time_left = maxf(burning.time_left, time)
	burning.pyre = maxf(burning.pyre, pyre_radius)


static func alive(node) -> bool:
	return is_instance_valid(node) and node.is_inside_tree() and node.is_in_group("enemies") and node.get("hp") > 0


func _ready() -> void:
	_target = get_parent()
	z_index = 1
	var radius = _target.get("hit_radius")
	var size: float = clampf((radius if radius != null else 14.0) / 14.0, 0.8, 2.0) * 0.8
	var sprite := AnimatedSprite2D.new()
	sprite.sprite_frames = FRAMES
	sprite.material = UNSHADED
	sprite.self_modulate = Color(1.5, 1.35, 1.2, 0.9)
	sprite.scale = Vector2.ONE * size
	sprite.offset = Vector2(0, -13)
	sprite.position = Vector2(0, 8 * size)
	sprite.play(&"flame")
	sprite.frame = randi() % 8
	add_child(sprite)


func _process(delta: float) -> void:
	if not alive(_target):
		_burn_out()
		return
	time_left -= delta
	_tick -= delta
	if _tick <= 0.0:
		_tick += TICK
		_target.take_damage(maxi(1, roundi(dps * TICK)))
		Sound.PlayEvery("burn", global_position, 0.6)
		if not alive(_target):
			_burn_out()
			return
	if time_left <= 0.0:
		queue_free()


func _burn_out() -> void:
	set_process(false)
	if pyre > 0.0 and is_instance_valid(_target):
		var origin := _target.global_position
		var spread := false
		for node in get_tree().get_nodes_in_group("enemies"):
			if node != _target and alive(node) and origin.distance_to(node.global_position) <= pyre:
				Burning.apply(node, dps, duration, pyre)
				spread = true
		if spread:
			var fx := FX.instantiate()
			_target.get_parent().add_child(fx)
			fx.global_position = origin
			fx.call("play_fx", &"fire_burst")
			Sound.PlayAt("fire_cast", origin)
	queue_free()
