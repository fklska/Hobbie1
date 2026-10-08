extends NPCController
class_name MobController

const VIEW := 1000.0
const SEPARATION_RADIUS := 36.0

var _last_gap := -1.0


func _physics_process(delta: float):
	super(delta)
	if heuristic != "model":
		return
	var body: Mob = _player
	if body.dead:
		return
	reward -= 0.0005
	if Mob.alive(body.target):
		var gap := body.gap_to(body.target)
		if _last_gap >= 0.0:
			reward += clampf(_last_gap - gap, -10.0, 10.0) / 500.0
		_last_gap = gap
	else:
		_last_gap = -1.0


func _relative(point: Vector2) -> Vector2:
	return ((point - _player.global_position) / VIEW).limit_length(1.0)


func _nearest(group: StringName, living_only: bool) -> Node2D:
	var best: Node2D = null
	var best_distance := VIEW
	for node: Node2D in get_tree().get_nodes_in_group(group):
		if node == _player or (living_only and node is Building):
			continue
		var distance := _player.global_position.distance_to(node.global_position)
		if distance < best_distance:
			best = node
			best_distance = distance
	return best


func get_obs() -> Dictionary:
	var body: Mob = _player
	var target: Node2D = body.target if Mob.alive(body.target) else null
	var to_target := _relative(Mob.target_point(target)) if target else Vector2.ZERO
	var gap := body.gap_to(target) if target else VIEW
	var threat := _nearest(&"village", true)
	var ally := _nearest(&"enemies", false)
	var to_threat := _relative(threat.global_position) if threat else Vector2.ZERO
	var to_ally := _relative(ally.global_position) if ally else Vector2.ZERO
	var to_base := _relative(Mob.base_point())
	return {"obs": [
		to_target.x, to_target.y, clampf(gap / VIEW, 0.0, 1.0),
		1.0 if target is Building else 0.0,
		1.0 if target and gap <= body.attack_range else 0.0,
		to_base.x, to_base.y,
		to_threat.x, to_threat.y,
		to_ally.x, to_ally.y,
		float(body.hp) / body.max_hp,
		1.0 if body.attack_timer <= 0.0 and not body.attacking else 0.0,
		1.0 if body.ranged else 0.0,
		body.attack_range / VIEW,
		body.speed / 300.0,
		DayNightCycle.night,
	]}


func get_reward() -> float:
	return reward


func get_action_space() -> Dictionary:
	return {
		"move": {"size": 2, "action_type": "continuous"},
		"attack": {"size": 2, "action_type": "discrete"},
	}


func apply_action(action: Dictionary) -> void:
	var move: Array = action["move"]
	_player.move_input = Vector2(move[0], move[1])
	_player.attack_input = action["attack"]


func on_damage_dealt(amount: int):
	reward += amount / 10.0


func on_damage_taken(amount: int):
	reward -= amount / 20.0


func on_death():
	reward -= 2.0
	done = true
	needs_reset = true


func reset():
	super()
	_last_gap = -1.0


func heuristic_action() -> Dictionary:
	var body: Mob = _player
	var target := body.target
	if not Mob.alive(target):
		return {"move": [0.0, 0.0], "attack": 0}
	var to_target := Mob.target_point(target) - body.global_position
	var gap := body.gap_to(target)
	var direction := to_target.normalized()
	var move := Vector2.ZERO
	if body.ranged:
		if gap > body.attack_range * 0.85:
			move = direction
		elif gap < body.attack_range * 0.5:
			move = -direction
	elif gap > body.attack_range * 0.8:
		move = direction
	move = (move + _separation(body)).limit_length(1.0)
	var attack := 1 if gap <= body.attack_range and body.attack_timer <= 0.0 else 0
	return {"move": [move.x, move.y], "attack": attack}


func _separation(body: Mob) -> Vector2:
	var push := Vector2.ZERO
	for other: Node2D in get_tree().get_nodes_in_group(&"enemies"):
		if other == body:
			continue
		var offset := body.global_position - other.global_position
		var distance := offset.length()
		if distance > 0.01 and distance < SEPARATION_RADIUS:
			push += offset / distance * (1.0 - distance / SEPARATION_RADIUS)
	return push
