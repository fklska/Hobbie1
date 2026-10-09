extends AIController2D
class_name NPCController

@export var decision_interval := 4
@export var deterministic := true

static var _models := {}

var _frame := 0
var _last_action := {}
var _own_model: ONNXModel
var _rng := RandomNumberGenerator.new()

func _physics_process(delta: float):
	super(delta)
	if heuristic != "human":
		return
	_frame += 1
	if _frame % decision_interval == 0:
		set_action(_infer() if _load_model() else heuristic_action())

func set_action(action = null) -> void:
	if action == null:
		action = heuristic_action()
	_last_action = action
	apply_action(action)

func get_action() -> Array:
	var result := []
	for key in get_action_space():
		var value = _last_action.get(key)
		if value is Array:
			result.append_array(value)
		else:
			result.append(value)
	return result

func heuristic_action() -> Dictionary:
	assert(false, "heuristic_action is not implemented")
	return {}

func apply_action(_action: Dictionary) -> void:
	assert(false, "apply_action is not implemented")

func _load_model() -> bool:
	if _own_model:
		return true
	if onnx_model_path.is_empty() or OS.has_feature("ios") or not FileAccess.file_exists(onnx_model_path):
		return false
	if not _models.has(onnx_model_path):
		var model := ONNXModel.new(onnx_model_path, 1)
		model.set_action_means_only(get_action_space())
		_models[onnx_model_path] = model
	_own_model = _models[onnx_model_path]
	return true

func _infer() -> Dictionary:
	var inference := _own_model.run_inference(get_obs(), 1)
	if not inference.has("output"):
		return heuristic_action()
	var output: Array = inference["output"]
	var result := {}
	var index := 0
	for key in get_action_space():
		var space: Dictionary = get_action_space()[key]
		var size: int = space["size"]
		if space["action_type"] == "discrete":
			result[key] = _pick(output.slice(index, index + size))
			index += size
		else:
			var values := []
			for i in size:
				values.append(clampf(output[index + i], -1.0, 1.0))
			result[key] = values
			index += size if _own_model.action_means_only else size * 2
	return result

func _pick(logits: Array) -> int:
	var best := 0
	for i in logits.size():
		if logits[i] > logits[best]:
			best = i
	if deterministic:
		return best
	var weights := PackedFloat32Array()
	for logit in logits:
		weights.append(exp(logit - logits[best]))
	return _rng.rand_weighted(weights)
