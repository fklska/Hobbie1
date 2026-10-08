extends Node2D
class_name LootPickup

const MAGNET_RANGE := 140.0
const PICK_RANGE := 26.0
const MAGNET_SPEED := 420.0
const COIN_LIFETIME := 180.0
const ARTIFACT_COLOR := Color(0.75, 0.45, 1.0)
const COIN_COLOR := Color(1.0, 0.82, 0.3)

@export var coins := 0
@export var artifact := ""

var _age := 0.0
var _bob := randf() * TAU


func _physics_process(delta: float):
	_age += delta
	_bob += delta * 3.0
	queue_redraw()
	if artifact.is_empty() and _age > COIN_LIFETIME:
		queue_free()
		return
	var hero = Game.Player
	if not is_instance_valid(hero) or hero.IsDead or Game.Survival == null:
		return
	var to_hero: Vector2 = hero.global_position - global_position
	var distance := to_hero.length()
	if distance <= PICK_RANGE:
		_collect()
	elif distance <= MAGNET_RANGE and _age > 0.4:
		global_position += to_hero / distance * minf(MAGNET_SPEED * delta, distance)


func _collect():
	if artifact.is_empty():
		Game.Survival.CollectCoins(coins)
	else:
		Game.Survival.CollectArtifact(artifact)
	queue_free()


func _draw():
	if has_node(^"Sprite"):
		return
	var lift := Vector2(0, -4.0 - sin(_bob) * 3.0)
	draw_circle(Vector2(0, 2), 7.0, Color(0, 0, 0, 0.25))
	if artifact.is_empty():
		var piles := clampi(coins, 1, 3)
		for i in piles:
			var offset := lift + Vector2((i - (piles - 1) / 2.0) * 7.0, -i % 2 * 3.0)
			draw_circle(offset, 5.0, COIN_COLOR.darkened(0.35))
			draw_circle(offset, 4.0, COIN_COLOR)
	else:
		var glow := 0.5 + 0.5 * sin(_bob * 1.7)
		draw_circle(lift, 12.0, Color(ARTIFACT_COLOR, 0.15 + 0.15 * glow))
		var diamond := PackedVector2Array([lift + Vector2(0, -9), lift + Vector2(7, 0), lift + Vector2(0, 9), lift + Vector2(-7, 0)])
		draw_colored_polygon(diamond, ARTIFACT_COLOR)
		draw_polyline(diamond + PackedVector2Array([diamond[0]]), Color.WHITE, 1.0)
