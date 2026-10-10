extends Node2D

const WARN_TIME := 1.6
const FALL_TIME := 0.8
const START := Vector2(180, -520)
const HARVEST_RADIUS := 115.0
const CRATER_TIME := 20.0
const FX := preload("res://Game/Skills/spell_fx.tscn")
const CRATER := preload("res://Art/effects/crater.png")

@onready var rock: AnimatedSprite2D = $Rock
@onready var trail: CPUParticles2D = $Rock/Trail
@onready var light: PointLight2D = $Light

var _damage := 1
var _radius := 140.0
var _burn_dps := 0.0
var _burn_time := 0.0
var _pyre := 0.0
var _time := 0.0
var _landed := false


func drop(at: Vector2, damage: int, radius: float, burn_dps: float, burn_time: float, pyre: float) -> void:
	global_position = at
	_damage = damage
	_radius = radius
	_burn_dps = burn_dps
	_burn_time = burn_time
	_pyre = pyre
	Sound.PlayAt("meteor_fall", at)


func _ready() -> void:
	rock.hide()
	rock.rotation = (-START).angle() - PI / 2.0
	trail.emitting = false
	light.energy = 0.0


func _process(delta: float) -> void:
	if _landed:
		return
	_time += delta
	queue_redraw()
	var fall := clampf((_time - (WARN_TIME - FALL_TIME)) / FALL_TIME, 0.0, 1.0)
	if fall > 0.0:
		if not rock.visible:
			rock.show()
			trail.emitting = true
		rock.position = START * (1.0 - pow(fall, 1.3))
		light.position = rock.position
		light.energy = 0.6 + 1.2 * fall
	if _time >= WARN_TIME:
		_impact()


func _draw() -> void:
	if _landed:
		return
	var t := clampf(_time / WARN_TIME, 0.0, 1.0)
	var r := _radius * (0.25 + 0.75 * t)
	var shadow := PackedVector2Array()
	for i in 40:
		shadow.append(Vector2.from_angle(TAU * i / 40.0) * Vector2(r, r * 0.5))
	draw_colored_polygon(shadow, Color(0.05, 0.02, 0.02, 0.2 + 0.45 * t))
	var pulse := 0.5 + 0.5 * sin(_time * 14.0)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2(1.0, 0.5))
	draw_arc(Vector2.ZERO, _radius, 0.0, TAU, 64, Color(1.0, 0.3, 0.15, 0.35 + 0.4 * pulse * t), 3.0, true)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


func _impact() -> void:
	_landed = true
	queue_redraw()
	rock.hide()
	trail.emitting = false
	var parent := get_parent()
	var origin := global_position

	var crater := Sprite2D.new()
	crater.texture = CRATER
	crater.z_index = 1
	crater.scale = Vector2.ONE * (_radius / 88.0)
	parent.add_child(crater)
	crater.global_position = origin
	var fade := crater.create_tween()
	fade.tween_interval(CRATER_TIME * 0.6)
	fade.tween_property(crater, "modulate:a", 0.0, CRATER_TIME * 0.4)
	fade.tween_callback(crater.queue_free)

	var fx := FX.instantiate()
	parent.add_child(fx)
	fx.global_position = origin
	fx.scale = Vector2.ONE * (_radius / 120.0)
	fx.call("play_fx", &"explosion")
	$Sparks.emitting = true

	Sound.PlayAt("meteor_impact", origin)
	if is_instance_valid(Game.Player):
		Game.Player.Shake(16.0, 0.7)

	for node in get_tree().get_nodes_in_group("enemies"):
		if not Burning.alive(node):
			continue
		var reach: float = _radius + (node.get("hit_radius") if node.get("hit_radius") != null else 16.0)
		var dist: float = node.global_position.distance_to(origin)
		if dist > reach:
			continue
		Game.Damage(node, maxi(1, roundi(_damage * (1.0 - 0.4 * dist / reach))))
		Burning.apply(node, _burn_dps, _burn_time, _pyre)
		_push(node, origin)
	_burn_resources(origin)

	light.position = Vector2.ZERO
	light.texture_scale = 4.0
	var flash := create_tween()
	flash.tween_property(light, "energy", 0.0, 0.9).from(1.6)
	flash.tween_interval(1.2)
	flash.tween_callback(queue_free)


func _push(node: Node2D, origin: Vector2) -> void:
	if node.get("mob_id") == null or node.get("hp") <= 0:
		return
	var away := node.global_position - origin
	var to := node.global_position + (away.normalized() if away.length() > 1.0 else Vector2.RIGHT) * 90.0
	if is_instance_valid(Game.World) and Game.World.IsOcean(to):
		return
	node.create_tween().tween_property(node, "global_position", to, 0.2).set_ease(Tween.EASE_OUT)


func _burn_resources(origin: Vector2) -> void:
	if not is_instance_valid(Game.World):
		return
	var tile := 64.0
	var lo := Vector2i(floori((origin.x - HARVEST_RADIUS) / tile), floori((origin.y - HARVEST_RADIUS) / tile))
	var hi := Vector2i(floori((origin.x + HARVEST_RADIUS) / tile), floori((origin.y + HARVEST_RADIUS) / tile))
	for x in range(lo.x, hi.x + 1):
		for y in range(lo.y, hi.y + 1):
			var cell := Vector2i(x, y)
			var center := (Vector2(cell) + Vector2(0.5, 0.5)) * tile
			if center.distance_to(origin) > HARVEST_RADIUS:
				continue
			if Game.World.GetResourceAt(cell) != 0:
				Game.World.HarvestTile(cell)
