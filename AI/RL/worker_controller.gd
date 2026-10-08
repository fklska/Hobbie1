extends NPCController
class_name WorkerController

const RAY_COUNT := 5
const RAY_CONE := 145.0
const RAY_LENGTH := 1000.0
const SEARCH_RADIUS := 30
const MOVE_CHECK_FRAMES := 500
const MOVE_CHECK_DISTANCE := 750.0

var _frames := 0
var _last_position := Vector2.ZERO
var _target := Vector2i(-1, -1)

func _physics_process(delta: float):
	super(delta)
	var body: Worker = _player
	if body.is_carrying():
		return
	reward -= 0.001
	if body.on_cell and body.act == 4:
		reward += 0.005
	_frames += 1
	if _frames >= MOVE_CHECK_FRAMES:
		if body.global_position.distance_to(_last_position) > MOVE_CHECK_DISTANCE:
			reward += 0.5
		_last_position = body.global_position
		_frames = 0
	if body.touched_wall:
		body.touched_wall = false
		reward -= 5.0
		needs_reset = true

func get_obs() -> Dictionary:
	var body: Worker = _player
	var bounds := body.area()
	var normalized := (body.global_position - bounds.position) / bounds.size
	var obs := [normalized.x, normalized.y, 1.0 if body.on_cell else 0.0]
	obs.append_array(_rays(body, bounds))
	return {"obs": obs}

func _rays(body: Worker, bounds: Rect2) -> Array:
	var result := []
	var world = Game.World
	var step := RAY_CONE / RAY_COUNT
	for i in RAY_COUNT:
		var direction := Vector2.from_angle(body.heading + deg_to_rad(step / 2 - RAY_CONE / 2 + i * step))
		var wall := _wall_distance(body.global_position - bounds.position, direction, bounds.size.x)
		var resource: float = world.CastResourceRay(body.global_position, direction, RAY_LENGTH, Game.WorkerJob) if world else -1.0
		if resource >= 0.0 and (wall < 0.0 or resource <= wall):
			result.append_array([0.0, 1.0, 1.0, resource / RAY_LENGTH])
		elif wall >= 0.0:
			result.append_array([1.0, 0.0, 1.0, wall / RAY_LENGTH])
		else:
			result.append_array([0.0, 0.0, 0.0, 1.0])
	return result

static func _wall_distance(local: Vector2, direction: Vector2, size: float) -> float:
	var distance := INF
	if direction.x > 0.0:
		distance = minf(distance, (size - local.x) / direction.x)
	elif direction.x < 0.0:
		distance = minf(distance, -local.x / direction.x)
	if direction.y > 0.0:
		distance = minf(distance, (size - local.y) / direction.y)
	elif direction.y < 0.0:
		distance = minf(distance, -local.y / direction.y)
	return maxf(distance, 0.0) if distance <= RAY_LENGTH else -1.0

func get_reward() -> float:
	return reward

func get_action_space() -> Dictionary:
	return {"rotate": {"size": 5, "action_type": "discrete"}}

func apply_action(action: Dictionary) -> void:
	_player.act = action["rotate"]

func on_collect():
	reward += 1.0

func reset():
	super()
	_frames = 0
	_last_position = _player.global_position
	_target = Vector2i(-1, -1)

func heuristic_action() -> Dictionary:
	var body: Worker = _player
	var world = Game.World
	if body.on_cell or world == null:
		return {"rotate": 4}
	if _target.x < 0 or Game.GetKindOf(world.GetResourceAt(_target)) != Game.WorkerJob:
		_target = world.FindNearestResource(body.global_position, SEARCH_RADIUS, Game.WorkerJob, body.area())
		if _target.x < 0:
			return {"rotate": 4}
	var to_target := Worker.cell_center(_target) - body.global_position
	var turn := wrapf(to_target.angle() - body.heading, -PI, PI)
	if absf(turn) > 0.25:
		return {"rotate": 2 if turn > 0.0 else 3}
	return {"rotate": 0}
