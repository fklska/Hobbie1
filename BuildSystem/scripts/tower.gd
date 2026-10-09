extends Building

const COOLDOWN := 1.2
const ARROW_SPEED := 900.0

var _timer := 0.0

func _physics_process(delta: float):
	_timer -= delta
	if _timer > 0:
		return
	var stats: Dictionary = Game.GetLevelStats(building_id)
	var target := _nearest_enemy(stats.range)
	if target == null:
		return
	_timer = COOLDOWN
	_shoot(target, stats.damage)

func _nearest_enemy(max_range: float) -> Node2D:
	var best: Node2D = null
	var best_distance := max_range
	for node in get_tree().get_nodes_in_group("enemies"):
		if not node is Node2D:
			continue
		var distance := get_center().distance_to(node.global_position)
		if distance < best_distance:
			best = node
			best_distance = distance
	return best

func _shoot(target: Node2D, damage: int):
	var from := get_center() - Vector2(0, footprint.y * 48)
	var arrow := Line2D.new()
	arrow.width = 2
	arrow.default_color = Color(0.35, 0.22, 0.12)
	arrow.points = [Vector2(-8, 0), Vector2(8, 0)]
	arrow.top_level = true
	arrow.z_index = 10
	arrow.global_position = from
	arrow.rotation = (target.global_position - from).angle()
	add_child(arrow)
	var tween := arrow.create_tween()
	tween.tween_property(arrow, "global_position", target.global_position, from.distance_to(target.global_position) / ARROW_SPEED)
	tween.tween_callback(func():
		if is_instance_valid(target):
			Game.Damage(target, damage)
		arrow.queue_free())
