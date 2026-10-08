extends KinematicBodyEntity
class_name StoneGiant

signal died

@export var data: BasicEnemyData
@export var hp_per_health := 6
@export var speed := 60.0
@export var aggro_range := 600.0
@export_category("Melee")
@export var melee_damage := 15
@export var melee_range := 120.0
@export var melee_cooldown := 2.0
@export_category("Laser")
@export var laser_damage := 20
@export var laser_range := 480.0
@export var laser_cooldown := 5.0
@export var laser_charge_time := 0.9
@export var hit_radius := 40.0

const LASER_SWEEP := 2.82743
const LASER_TIME := 0.65
const BEAM_HALF_WIDTH := 24.0
const ENRAGE_SPEED := 1.4
const ENRAGE_COOLDOWN := 0.6

enum {
	IDLE,
	WALK,
	ATTACK,
	ARMOR,
	DIE
}

@onready var anim_player: AnimationPlayer = $AnimationPlayer
@onready var laser_pivot: Node2D = $LaserPivot
@onready var laser: AnimatedSprite2D = $LaserPivot/Laser
@onready var ai: BossController = $AIController2D

var state = WALK
var max_hp: int
var hp: int
var move_input := Vector2.ZERO
var attack_input := 0
var melee_timer := 0.0
var laser_timer := 2.0
var enraged := false
var firing := false
var _laser_hits := {}

func _ready():
	super()
	max_hp = data.HEALTH * hp_per_health
	hp = max_hp
	add_to_group("enemies")
	ai.init(self)
	anim.play("idle")

func _physics_process(delta):
	if state == DIE:
		return
	melee_timer -= delta
	laser_timer -= delta
	if firing:
		_laser_tick()
	if state != WALK:
		return

	if attack_input == 1 and melee_timer <= 0:
		melee()
	elif attack_input == 2 and laser_timer <= 0:
		fire_laser()
	else:
		velocity = move_input.limit_length(1.0) * speed * (ENRAGE_SPEED if enraged else 1.0)
		move_and_slide()
		if absf(velocity.x) > 1:
			anim.flip_h = velocity.x < 0

func cooldown_scale() -> float:
	return ENRAGE_COOLDOWN if enraged else 1.0

func focus_target() -> Node2D:
	var best: Node2D = null
	var best_distance := aggro_range
	for node: Node2D in get_tree().get_nodes_in_group("village"):
		if node is Building:
			continue
		var distance := global_position.distance_to(node.global_position)
		if distance < best_distance:
			best = node
			best_distance = distance
	if best:
		return best
	var main_base = Game.MainBase
	return main_base if is_instance_valid(main_base) else null

static func target_point(node: Node2D) -> Vector2:
	return (node as Building).get_center() if node is Building else node.global_position

static func target_radius(node: Node2D) -> float:
	return (node as Building).footprint.x * 32.0 if node is Building else 16.0

func melee():
	state = ATTACK
	melee_timer = melee_cooldown * cooldown_scale()
	anim.play("melee")
	await get_tree().create_timer(0.4, false).timeout
	if state == DIE:
		return
	for node: Node2D in get_tree().get_nodes_in_group("village"):
		if global_position.distance_to(target_point(node)) <= melee_range + target_radius(node):
			_hit(node, melee_damage)
	await get_tree().create_timer(0.3, false).timeout
	_finish_attack()

func fire_laser():
	var target := focus_target()
	if target == null:
		return
	state = ATTACK
	laser_timer = laser_cooldown * cooldown_scale()
	anim.play("cast")
	laser_pivot.rotation = (target_point(target) - laser_pivot.global_position).angle() - LASER_SWEEP / 2
	laser.visible = true
	anim_player.play("pre_laser")
	await get_tree().create_timer(laser_charge_time * cooldown_scale(), false).timeout
	if state == DIE:
		return
	_laser_hits.clear()
	firing = true
	anim_player.play("laser")
	await get_tree().create_timer(LASER_TIME, false).timeout
	firing = false
	laser.visible = false
	anim_player.play("RESET")
	_finish_attack()

func _laser_tick():
	var origin := laser_pivot.global_position
	var direction := Vector2.RIGHT.rotated(laser.global_rotation)
	var length := laser_range
	for node: Node2D in get_tree().get_nodes_in_group("village"):
		if _laser_hits.has(node):
			continue
		var point := target_point(node)
		var along := clampf((point - origin).dot(direction), 0.0, length)
		if point.distance_to(origin + direction * along) <= BEAM_HALF_WIDTH + target_radius(node):
			_laser_hits[node] = true
			_hit(node, laser_damage)

func _hit(node: Node, amount: int):
	Game.Damage(node, amount)
	ai.on_damage_dealt(amount)

func _finish_attack():
	if state == ATTACK:
		state = WALK
		anim.play("idle")

func take_damage(amount: int):
	if state == DIE or state == ARMOR:
		return
	hp = max(0, hp - amount)
	ai.on_damage_taken(amount)
	modulate = Color(1, 0.5, 0.5)
	create_tween().tween_property(self, "modulate", Color.WHITE, 0.15)
	if hp == 0:
		die()
	elif not enraged and hp <= max_hp / 2:
		enrage()

func enrage():
	enraged = true
	firing = false
	laser.visible = false
	state = ARMOR
	anim.play("armor")
	await get_tree().create_timer(1.0, false).timeout
	if state == ARMOR:
		state = WALK
		anim.play("idle")

func die():
	state = DIE
	firing = false
	laser.visible = false
	velocity = Vector2.ZERO
	anim.play("death")
	ai.done = true
	await get_tree().create_timer(1.4, false).timeout
	died.emit()

func get_texture():
	return anim.sprite_frames.get_frame_texture("idle", 0)

func send_obj_data() -> Dictionary:
	return {
		"Name": data.NAME,
		"HP": "%d/%d" % [hp, max_hp]
	}
