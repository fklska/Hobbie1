extends KinematicBodyEntity
class_name Mob

signal died(mob: Mob)

const DIRECTIONS := ["e", "se", "s", "sw", "w", "nw", "n", "ne"]
const ARROW := preload("res://AI/Enemies/arrow.tscn")
const SPAWN_RING := Vector2(700.0, 1100.0)

@export var mob_id := "goblin"
@export var title := "Гоблин"
@export_multiline var description := ""
@export var max_hp := 30
@export var speed := 110.0
@export var damage := 5
@export var attack_range := 32.0
@export var attack_cooldown := 1.0
@export var attack_windup := 0.25
@export var aggro_range := 450.0
@export var ranged := false
@export var prefers_buildings := false
@export var hit_radius := 14.0
@export var coins := Vector2i(1, 3)
@export_range(0.0, 1.0) var artifact_chance := 0.02
@export var faces_left := false

@onready var ai: MobController = $AIController2D

var hp: int
var move_input := Vector2.ZERO
var attack_input := 0
var attack_timer := 0.0
var attacking := false
var dead := false
var facing := Vector2.RIGHT
var target: Node2D
var _retarget := 0.0


func _ready():
	super()
	hp = max_hp
	add_to_group("enemies")
	ai.init(self)
	_play(&"idle")


func _physics_process(delta: float):
	if ai.needs_reset and ai.heuristic == "model":
		reset_episode()
		return
	if dead:
		return
	attack_timer -= delta
	_retarget -= delta
	if _retarget <= 0.0 or not alive(target):
		target = pick_target()
		_retarget = 0.5
	if attacking:
		velocity = Vector2.ZERO
	elif attack_input == 1 and attack_timer <= 0.0:
		velocity = Vector2.ZERO
		_attack()
	else:
		velocity = move_input.limit_length(1.0) * speed
		if velocity.length() > 5.0:
			facing = velocity.normalized()
	move_and_slide()
	if not attacking:
		_play(&"walk" if velocity.length() > 5.0 else &"idle")


static func alive(node) -> bool:
	return is_instance_valid(node) and node.is_inside_tree() and node.is_in_group("village")


static func target_point(node: Node2D) -> Vector2:
	return (node as Building).get_center() if node is Building else node.global_position


static func target_radius(node: Node2D) -> float:
	if node is Building:
		return (node as Building).footprint.x * 32.0
	var radius = node.get("hit_radius")
	return radius if radius != null else 16.0


static func base_point() -> Vector2:
	if alive(Game.MainBase):
		return Game.MainBase.get_center()
	if is_instance_valid(Game.Player):
		return Game.Player.global_position
	return Vector2.ZERO


func gap_to(node: Node2D) -> float:
	return global_position.distance_to(target_point(node)) - target_radius(node)


func in_reach(node: Node2D) -> bool:
	return alive(node) and gap_to(node) <= attack_range


func pick_target() -> Node2D:
	var best: Node2D = null
	var best_score := INF
	for node: Node2D in get_tree().get_nodes_in_group("village"):
		var gap := gap_to(node)
		if gap > aggro_range:
			continue
		var score := gap
		if node is Building:
			score *= 0.5 if prefers_buildings else 2.0
		if score < best_score:
			best = node
			best_score = score
	if best:
		return best
	if alive(Game.MainBase):
		return Game.MainBase
	for node: Node2D in get_tree().get_nodes_in_group("village"):
		var gap := gap_to(node)
		if gap < best_score:
			best = node
			best_score = gap
	return best


func _attack():
	attacking = true
	attack_timer = attack_cooldown
	var victim := target
	if alive(victim):
		facing = (target_point(victim) - global_position).normalized()
	_play(&"attack", true)
	await get_tree().create_timer(attack_windup, false).timeout
	if dead:
		return
	if in_reach(victim):
		if ranged:
			_shoot(victim)
		else:
			Game.Damage(victim, damage)
			ai.on_damage_dealt(damage)
	await get_tree().create_timer(0.2, false).timeout
	attacking = false


func _shoot(victim: Node2D):
	var arrow: Arrow = ARROW.instantiate()
	arrow.position = position
	arrow.setup(target_point(victim) - global_position, damage, &"village", self, attack_range * 1.4)
	get_parent().add_child(arrow)


func on_projectile_hit(amount: int):
	ai.on_damage_dealt(amount)


func take_damage(amount: int):
	if dead:
		return
	hp = maxi(0, hp - amount)
	ai.on_damage_taken(amount)
	modulate = Color(1, 0.5, 0.5)
	create_tween().tween_property(self, "modulate", Color.WHITE, 0.15)
	if hp == 0:
		die()


func die():
	dead = true
	attacking = false
	velocity = Vector2.ZERO
	remove_from_group("enemies")
	ai.on_death()
	died.emit(self)
	if ai.heuristic == "model":
		return
	collision_layer = 0
	input_pickable = false
	var tween := create_tween()
	if _resolve(&"death").is_empty():
		tween.tween_property(anim, "rotation", PI / 2 * (-1.0 if facing.x < 0.0 else 1.0), 0.25)
	else:
		_play(&"death", true)
	tween.tween_property(self, "modulate:a", 0.0, 0.6).set_delay(0.6)
	tween.tween_callback(queue_free)


func reset_episode():
	global_position = base_point() + Vector2.from_angle(randf() * TAU) * randf_range(SPAWN_RING.x, SPAWN_RING.y)
	hp = max_hp
	dead = false
	attacking = false
	attack_timer = 0.0
	target = null
	move_input = Vector2.ZERO
	attack_input = 0
	modulate = Color.WHITE
	anim.rotation = 0.0
	if not is_in_group("enemies"):
		add_to_group("enemies")
	ai.reset()
	ai.done = true


func _resolve(base: StringName) -> String:
	var frames := anim.sprite_frames
	var directed := "%s_%s" % [base, DIRECTIONS[wrapi(roundi(facing.angle() / (PI / 4.0)), 0, 8)]]
	if frames.has_animation(directed):
		return directed
	if frames.has_animation(base):
		return base
	return ""


func _play(base: StringName, restart := false):
	var animation := _resolve(base)
	if animation.is_empty():
		return
	anim.flip_h = animation == String(base) and (facing.x < 0.0) != faces_left
	if restart or anim.animation != animation:
		anim.play(animation)
		if restart:
			anim.frame = 0


func get_texture():
	var animation := _resolve(&"idle")
	return anim.sprite_frames.get_frame_texture(animation, 0) if not animation.is_empty() else null


func send_obj_data() -> Dictionary:
	return {
		"Name": title,
		"Description": description,
		"HP": "%d/%d" % [hp, max_hp]
	}
