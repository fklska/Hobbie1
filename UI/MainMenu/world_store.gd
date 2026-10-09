class_name WorldStore
extends RefCounted

const DIR := "user://SavedWorlds/"
const RUNS_DIR := "user://Runs/"


static func list_worlds() -> Array[SimpleGeneratorData]:
	var result: Array[SimpleGeneratorData] = []
	DirAccess.make_dir_recursive_absolute(DIR)
	for file_name in DirAccess.get_files_at(DIR):
		if not (file_name.begins_with("__SIMPLE") and file_name.ends_with(".tres")):
			continue
		var data := ResourceLoader.load(DIR + file_name, "", ResourceLoader.CACHE_MODE_REPLACE) as SimpleGeneratorData
		if data != null:
			result.append(data)
	result.sort_custom(func(a: SimpleGeneratorData, b: SimpleGeneratorData) -> bool: return modified_time(a) > modified_time(b))
	return result


static func scene_path(world: SimpleGeneratorData) -> String:
	return world.fullDataPath.get_basename() + ".tscn"


static func simple_path(world: SimpleGeneratorData) -> String:
	return world.fullDataPath.get_base_dir().path_join("__SIMPLE" + world.fullDataPath.get_file())


static func modified_time(world: SimpleGeneratorData) -> int:
	return FileAccess.get_modified_time(world.fullDataPath)


static func find(path: String) -> SimpleGeneratorData:
	if path == "":
		return null
	for world in list_worlds():
		if scene_path(world) == path:
			return world
	return null


static func exists(world_name: String) -> bool:
	return FileAccess.file_exists(DIR + world_name + ".tres")


static func run_path(world: SimpleGeneratorData) -> String:
	return RUNS_DIR + scene_path(world).get_file().get_basename() + ".json"


static func has_run(world: SimpleGeneratorData) -> bool:
	return Game.has_method("HasRun") and Game.HasRun(scene_path(world))


static func run_day(world: SimpleGeneratorData) -> int:
	if not has_run(world) or not Game.has_method("RunDay"):
		return 0
	return Game.RunDay(scene_path(world))


static func play(world: SimpleGeneratorData, fresh := false) -> void:
	Overlay.start_world(scene_path(world), world.SpawnPoint, has_run(world) and not fresh)


static func delete_run(world: SimpleGeneratorData) -> void:
	if FileAccess.file_exists(run_path(world)):
		DirAccess.remove_absolute(run_path(world))


static func delete(world: SimpleGeneratorData) -> void:
	for path in [world.fullDataPath, scene_path(world), simple_path(world), run_path(world)]:
		if FileAccess.file_exists(path):
			DirAccess.remove_absolute(path)
	if GameSettings.last_world() == scene_path(world):
		GameSettings.set_last_world("")
