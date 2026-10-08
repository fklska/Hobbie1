extends Node2D

const SAVED_WORLDS_DIR := "user://SavedWorlds/"

@export var world_name := ""
@export var worker_count := 8
@export var restore_interval := 60.0

var _world: WorldScene
var _harvested := {}
var _restore_timer := 0.0

func _ready():
	var data := _find_world()
	if data == null:
		push_error("Нет сохранённых миров в %s: сгенерируйте мир через меню игры" % SAVED_WORLDS_DIR)
		return
	_world = load(data.fullDataPath.get_basename() + ".tscn").instantiate()
	add_child(_world)
	_world.TileHarvested.connect(func(cell, type): _harvested[cell] = type)
	Game.ResetState()
	Game.World = _world
	Game.AddResource("wood", 10)
	Game.AddResource("stone", 5)
	Game.PlaceBuilding("TownHall", Vector2i(data.SpawnPoint / 64))

	var center: Vector2 = Game.TownHall.get_center()
	$Camera2D.position = center
	for i in worker_count:
		var worker: Node2D = load("res://AI/Village/worker.tscn").instantiate()
		worker.position = center + Vector2.from_angle(TAU * i / worker_count) * 120.0
		_world.get_node("Enviroment").add_child(worker)

func _find_world() -> SimpleGeneratorData:
	for file in DirAccess.get_files_at(SAVED_WORLDS_DIR):
		if file.begins_with("__SIMPLE") and (world_name.is_empty() or file == "__SIMPLE%s.tres" % world_name):
			return load(SAVED_WORLDS_DIR + file)
	return null

func _physics_process(delta: float):
	_restore_timer += delta
	if _restore_timer < restore_interval or _world == null:
		return
	_restore_timer = 0.0
	for cell in _harvested:
		_world.RestoreTile(cell, _harvested[cell])
	_harvested.clear()
