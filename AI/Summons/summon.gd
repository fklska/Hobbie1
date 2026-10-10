extends KinematicBodyEntity
class_name Summon

const FOLLOW_DISTANCE := 80.0
const LEASH := 650.0
const FADE_TIME := 3.0
const BAR_WIDTH := 26.0

@export var kind := "crow"
@export var title := "Ворон"
@export_multiline var description := ""
@export var speed := 220.0
@export var attack_range := 28.0
@export var attack_cooldown := 0.8
@export var attack_windup := 0.15
@export var aggro_range := 420.0
@export var flying := false
@export var targetable := true
@export var area_attack := false
@export var hit_radius := 12.0
@export var bar_height := 40.0

@onready var body_sprite: DirectionalSprite = $AnimatedSprite2D

var hp := 1
var max_hp := 1
var damage := 1
var life := 30.0
var dead := false
var target: Node2D
var _attack_timer := 0.0
var _retarget := 0.0
var _attacking := false
var _index := 0
var _facing := Vector2.DOWN


func setup(health: int, dmg: int, lifetime: float, index: int) -> void:
	max_hp = health
	hp = health
	damage = dmg
	life = lifetime
	_index = index


func _ready():
	super()
	add_to_group("summons")
	add_to_group("summon_" + kind)
	if targetable:
		add_to_group("village")
	shader.set_shader_parameter("color", Color(0.55, 1.0, 0.45, 0.8))
	shader.set_shader_parameter("enable", true)
	body_sprite.play_dir("idle", _facing)


func set_outline():
	shader.set_shader_parameter("color", Color(1, 1, 1, 1))


func hide_outline():
	shader.set_shader_parameter("color", Color(0.55, 1.0, 0.45, 0.8))


func _physics_process(delta: float) -> void:
	if dead:
		return
	life -= delta
	if life <= 0.0:
		dismiss()
		return
	modulate.a = 1.0 if life > FADE_TIME else 0.55 + 0.45 * absf(sin(life * 8.0))
	_attack_timer -= delta
	_retarget -= delta
	var hero: Node2D = Game.Player if is_instance_valid(Game.Player) else null
	if _retarget <= 0.0 or not _hostile(target):
		target = _find_target(hero)
		_retarget = 0.3
	if _attacking:
		velocity = Vector2.ZERO
	elif target:
		var gap := global_position.distance_to(target.global_position) - _radius(target)
		if gap <= attack_range:
			velocity = Vector2.ZERO
			_facing = global_position.direction_to(target.global_position)
			if _attack_timer <= 0.0:
				_attack(target)
		else:
			_move_to(target.global_position)
	elif hero:
		var spot := hero.global_position + Vector2.from_angle(PI / 2.0 + (_index - 1) * 0.9 + (PI if flying else 0.0)) * FOLLOW_DISTANCE
		if global_position.distance_to(spot) > 24.0:
			_move_to(spot, global_position.distance_to(spot) > 300.0)
		else:
			velocity = Vector2.ZERO
	else:
		velocity = Vector2.ZERO
	if not flying:
		keep_off_ocean(delta)
	move_and_slide()
	if not _attacking:
		body_sprite.play_dir("walk" if velocity.length() > 5.0 else "idle", velocity if velocity.length() > 5.0 else _facing)
	queue_redraw()


func _move_to(point: Vector2, hurry := false) -> void:
	var dir := global_position.direction_to(point) if flying else steer_to(point)
	velocity = dir * speed * (1.4 if hurry else 1.0)
	if velocity.length() > 5.0:
		_facing = velocity.normalized()


func _hostile(node) -> bool:
	return is_instance_valid(node) and node.is_inside_tree() and node.is_in_group("enemies") and node.get("hp") > 0


func _radius(node: Node2D) -> float:
	var r = node.get("hit_radius")
	return r if r != null else 16.0


func _find_target(hero: Node2D) -> Node2D:
	var anchor := hero.global_position if hero else global_position
	if hero and global_position.distance_to(anchor) > LEASH:
		return null
	var best: Node2D = null
	var best_dist := INF
	for node in get_tree().get_nodes_in_group("enemies"):
		if not _hostile(node):
			continue
		var from_anchor: float = anchor.distance_to(node.global_position)
		if from_anchor > aggro_range + 120.0:
			continue
		var dist: float = global_position.distance_to(node.global_position)
		if dist < best_dist:
			best = node
			best_dist = dist
	return best


func _attack(victim: Node2D) -> void:
	_attacking = true
	_attack_timer = attack_cooldown
	body_sprite.play_dir("attack", _facing, true)
	await get_tree().create_timer(attack_windup, false).timeout
	if dead:
		return
	if area_attack:
		for node in get_tree().get_nodes_in_group("enemies"):
			if _hostile(node) and global_position.distance_to(node.global_position) - _radius(node) <= attack_range + 30.0 \
					and _facing.dot(global_position.direction_to(node.global_position)) > -0.2:
				Game.Damage(node, damage)
	elif _hostile(victim) and global_position.distance_to(victim.global_position) - _radius(victim) <= attack_range + 20.0:
		Game.Damage(victim, damage)
	if kind == "crow" and randf() < 0.3:
		Sound.PlayEvery("crow", global_position, 2.0)
	await get_tree().create_timer(maxf(0.1, body_sprite.action_length("attack") - attack_windup), false).timeout
	_attacking = false


func take_damage(amount: int) -> void:
	if dead:
		return
	hp = maxi(0, hp - amount)
	modulate = Color(1, 0.5, 0.5, modulate.a)
	create_tween().tween_property(self, "modulate", Color(1, 1, 1, modulate.a), 0.15)
	queue_redraw()
	if hp == 0:
		_die(true)


func dismiss() -> void:
	_die(false)


func _die(killed: bool) -> void:
	if dead:
		return
	dead = true
	velocity = Vector2.ZERO
	remove_from_group("village")
	remove_from_group("summons")
	remove_from_group("summon_" + kind)
	input_pickable = false
	queue_redraw()
	var tween := create_tween()
	if killed:
		body_sprite.play_dir("death", _facing, true)
		tween.tween_property(self, "modulate:a", 0.0, 0.5).set_delay(maxf(0.2, body_sprite.action_length("death")))
	else:
		tween.tween_property(self, "modulate", Color(0.6, 1.4, 0.6, 0.0), 0.5)
	tween.tween_callback(queue_free)


func _draw() -> void:
	if dead or hp >= max_hp:
		return
	var origin := Vector2(-BAR_WIDTH / 2.0, -bar_height)
	draw_rect(Rect2(origin - Vector2.ONE, Vector2(BAR_WIDTH + 2, 5)), Color(0.08, 0.05, 0.04, 0.9))
	draw_rect(Rect2(origin, Vector2(BAR_WIDTH * hp / max_hp, 3)), Color(0.45, 0.85, 0.35))


func get_texture():
	return anim.sprite_frames.get_frame_texture(body_sprite.anim_name("idle"), 0)


func send_obj_data() -> Dictionary:
	return {
		"Name": title,
		"Description": description,
		"HP": "%d/%d" % [hp, max_hp],
		"Урон": str(damage),
		"Осталось": "%d с" % ceili(life),
	}
