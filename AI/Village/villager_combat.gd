extends Node2D
class_name VillagerCombat

const ARROW := preload("res://AI/Enemies/arrow.tscn")
const SPEED := 170.0
const SELF_RANGE := 350.0
const GUARD_RADIUS := 700.0
const GUARD_SPOT := 160.0
const FLEE_RANGE := 280.0
const CALM_TIME := 3.0
const WEAPON_SIZE := 20.0

@onready var body: Worker = get_parent()
@onready var weapon_sprite: Sprite2D = $"../AnimatedSprite2D/Weapon"

var stats := {}
var target: Node2D
var cooldown := 0.0
var _retarget := 0.0
var _calm := 0.0
var _swing := 0.0


func is_armed() -> bool:
	return not stats.is_empty()


func is_ranged() -> bool:
	return stats.get("kind", "") == "ranged"


func equip(weapon: Dictionary):
	stats = weapon
	var icon: String = weapon.get("icon", "")
	var texture: Texture2D = load(icon) if not icon.is_empty() and ResourceLoader.exists(icon) else null
	weapon_sprite.texture = texture
	weapon_sprite.visible = texture != null
	if texture:
		weapon_sprite.scale = Vector2.ONE * WEAPON_SIZE / maxf(texture.get_width(), texture.get_height())
	target = null
	queue_redraw()


func update(delta: float) -> bool:
	cooldown -= delta
	_retarget -= delta
	if _swing > 0.0:
		_swing = maxf(_swing - delta * 5.0, 0.0)
		weapon_sprite.rotation = -_swing * 1.4
		queue_redraw()
	if _retarget <= 0.0 or not _hostile(target):
		target = _find_enemy()
		_retarget = 0.4

	if is_armed():
		if target:
			_fight()
			return true
		if _is_night() and Mob.alive(Game.MainBase):
			_go_to(_guard_spot(), 24.0)
			return true
		return false

	if target and body.global_position.distance_to(target.global_position) <= FLEE_RANGE:
		_calm = CALM_TIME
	else:
		_calm -= delta
	if _calm > 0.0:
		_flee()
		return true
	return false


func _hostile(node) -> bool:
	return is_instance_valid(node) and node.is_inside_tree() and node.is_in_group("enemies") and not node.get("dead")


func _is_night() -> bool:
	return DayNightCycle.night >= 0.5


func _find_enemy() -> Node2D:
	var guarding := is_armed() and Mob.alive(Game.MainBase)
	var base := Mob.base_point()
	var best: Node2D = null
	var best_distance := INF
	for enemy: Node2D in get_tree().get_nodes_in_group("enemies"):
		if not _hostile(enemy):
			continue
		var distance := body.global_position.distance_to(enemy.global_position)
		if distance > SELF_RANGE and not (guarding and enemy.global_position.distance_to(base) <= GUARD_RADIUS):
			continue
		if distance < best_distance:
			best = enemy
			best_distance = distance
	return best


func _fight():
	var to_enemy := target.global_position - body.global_position
	var gap := to_enemy.length() - Mob.target_radius(target)
	var reach: float = stats.get("range", 40.0)
	var direction := to_enemy.normalized()
	var path := body.steer_to(target.global_position)
	if is_ranged():
		if gap > reach * 0.9:
			_move(path)
		elif gap < reach * 0.4:
			_move(-direction)
		else:
			_move(Vector2.ZERO)
	else:
		_move(path if gap > reach * 0.8 else Vector2.ZERO)
	if gap <= reach and cooldown <= 0.0:
		_strike(direction)


func _strike(direction: Vector2):
	cooldown = stats.get("cooldown", 1.0)
	_swing = 1.0
	var multiplier: float = Game.Survival.VillagerDamageMultiplier if Game.Survival else 1.0
	var damage := roundi(stats.get("damage", 5) * multiplier)
	if not is_ranged():
		Game.Damage(target, damage)
		return
	var arrow: Arrow = ARROW.instantiate()
	arrow.position = body.position
	match stats.get("id", ""):
		"sling":
			arrow.style = Arrow.Style.STONE
		"musket":
			arrow.style = Arrow.Style.BULLET
	arrow.setup(direction, damage, &"enemies", self, stats.get("range", 300.0) * 1.3, stats.get("projectile_speed", 0.0))
	body.get_parent().add_child(arrow)


func _move(direction: Vector2):
	body.velocity = direction * SPEED
	body.current_velocity = body.velocity
	if direction != Vector2.ZERO:
		body.heading = direction.angle()


func _go_to(point: Vector2, tolerance: float):
	_move(body.steer_to(point) if body.global_position.distance_to(point) > tolerance else Vector2.ZERO)


func _guard_spot() -> Vector2:
	return Mob.base_point() + Vector2.from_angle(float(body.get_instance_id() % 628) / 100.0) * GUARD_SPOT


func _flee():
	if Mob.alive(Game.MainBase):
		_go_to(Mob.base_point() + Vector2.from_angle(float(body.get_instance_id() % 628) / 100.0) * 60.0, 16.0)
	elif _hostile(target):
		_move((body.global_position - target.global_position).normalized())
	else:
		_move(Vector2.ZERO)


func _draw():
	if not is_armed() or weapon_sprite.texture:
		return
	var hand := Vector2(8, 6)
	var wood := Color(0.45, 0.3, 0.15)
	if is_ranged():
		var tilt := -_swing * 0.4
		draw_arc(hand, 10.0, tilt - 1.2, tilt + 1.2, 12, wood, 2.0)
		draw_line(hand + Vector2.from_angle(tilt - 1.2) * 10.0, hand + Vector2.from_angle(tilt + 1.2) * 10.0, Color(0.9, 0.9, 0.85), 1.0)
	else:
		var tip := hand + Vector2.from_angle(-PI / 2 + 0.4 + _swing * 1.2) * 24.0
		draw_line(hand, tip, wood, 2.0)
		draw_circle(tip, 2.5, Color(0.75, 0.75, 0.8))
