extends NPCController
class_name BossController

const VIEW := 1000.0

func _relative(point: Vector2) -> Vector2:
	var offset: Vector2 = (point - _player.global_position) / VIEW
	return offset.limit_length(1.0)

func get_obs() -> Dictionary:
	var body: StoneGiant = _player
	var target := body.focus_target()
	var to_target := _relative(body.target_point(target)) if target else Vector2.ZERO
	var to_hall := _relative(body.target_point(Game.MainBase)) if is_instance_valid(Game.MainBase) else Vector2.ZERO
	var to_player := _relative(Game.Player.global_position) if is_instance_valid(Game.Player) else Vector2.ZERO
	return {"obs": [
		to_target.x, to_target.y, to_target.length(),
		to_hall.x, to_hall.y,
		to_player.x, to_player.y,
		float(body.hp) / body.max_hp,
		1.0 if body.melee_timer <= 0 else 0.0,
		1.0 if body.laser_timer <= 0 else 0.0,
		1.0 if body.enraged else 0.0,
		1.0 if body.state == body.WALK else 0.0,
	]}

func get_reward() -> float:
	return reward

func get_action_space() -> Dictionary:
	return {
		"move": {"size": 2, "action_type": "continuous"},
		"attack": {"size": 3, "action_type": "discrete"},
	}

func apply_action(action: Dictionary) -> void:
	var move: Array = action["move"]
	_player.move_input = Vector2(move[0], move[1])
	_player.attack_input = action["attack"]

func on_damage_dealt(amount: int):
	reward += amount / 10.0

func on_damage_taken(amount: int):
	reward -= amount / 20.0

func heuristic_action() -> Dictionary:
	var body: StoneGiant = _player
	var target := body.focus_target()
	if target == null:
		return {"move": [0.0, 0.0], "attack": 0}

	var to_target := body.target_point(target) - body.global_position
	var distance := to_target.length() - body.target_radius(target)
	var attack := 0
	if distance <= body.melee_range and body.melee_timer <= 0:
		attack = 1
	elif distance > body.melee_range * 0.8 and distance <= body.laser_range and body.laser_timer <= 0:
		attack = 2
	var move := to_target.normalized() if distance > body.melee_range * 0.7 else Vector2.ZERO
	return {"move": [move.x, move.y], "attack": attack}
