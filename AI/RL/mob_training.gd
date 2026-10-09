extends Node2D

const SAVED_WORLDS_DIR := "user://SavedWorlds/"
const WORKER := preload("res://AI/Village/worker.tscn")

@export var world_name := ""
@export var mob_scenes: Array[PackedScene] = []
@export var mobs_per_scene := 4
@export var episode_frames := 3600
@export var defender_count := 4
@export var defender_weapons: Array[String] = ["spear", "bow"]
@export_range(0.0, 1.0) var night := 1.0

var _world: WorldScene
var _center := Vector2.ZERO


func _ready():
	var data := _find_world()
	if data == null:
		push_error("Нет сохранённых миров в %s: сгенерируйте мир через меню игры" % SAVED_WORLDS_DIR)
		return
	var scene_path := data.fullDataPath.get_basename() + ".tscn"
	if GENERATOR.NeedsUpgrade(scene_path) and not GENERATOR.UpgradeWorldNow(scene_path):
		push_error("Не удалось перевести мир %s в новый формат" % scene_path)
		return
	_world = load(scene_path).instantiate()
	add_child(_world)
	Game.ResetState()
	Game.World = _world
	Game.AddResource("wood", 100)
	Game.AddResource("stone", 100)
	Game.PlaceBuilding("TownHall", Vector2i(_world.SpawnPosition() / 64))
	DayNightCycle.night = night

	_center = Game.MainBase.get_center()
	$Camera2D.position = _center
	for i in defender_count:
		_add_defender(i, false)
	for scene in mob_scenes:
		for i in mobs_per_scene:
			var mob: Mob = scene.instantiate()
			mob.position = _center + Vector2.from_angle(randf() * TAU) * randf_range(Mob.SPAWN_RING.x, Mob.SPAWN_RING.y)
			mob.get_node("AIController2D").reset_after = episode_frames
			_world.get_node("Enviroment").add_child(mob)


func _find_world() -> SimpleGeneratorData:
	for file in DirAccess.get_files_at(SAVED_WORLDS_DIR):
		if file.begins_with("__SIMPLE") and (world_name.is_empty() or file == "__SIMPLE%s.tres" % world_name):
			return load(SAVED_WORLDS_DIR + file)
	return null


func _add_defender(index: int, late: bool):
	var worker: Worker = WORKER.instantiate()
	worker.position = _center + Vector2.from_angle(TAU * index / defender_count) * 120.0
	if not late:
		worker.get_node("AIController2D").control_mode = AIController2D.ControlModes.HUMAN
	_world.get_node("Enviroment").add_child(worker)
	if not defender_weapons.is_empty():
		var weapon: String = defender_weapons[index % defender_weapons.size()]
		worker.equip(weapon, Game.WeaponStats(weapon))


func _physics_process(_delta: float):
	if _world == null:
		return
	var base = Game.MainBase
	if is_instance_valid(base) and base.hp < base.max_hp / 4:
		base.heal(base.max_hp)
	var defenders := get_tree().get_nodes_in_group("village").filter(func(node): return node is Worker)
	for i in range(defenders.size(), defender_count):
		_add_defender(i, true)
