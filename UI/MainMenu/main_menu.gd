@tool
extends Control
class_name MainMenu

@export_dir var saveFolderPath: String = "res://SavedWorlds/"
var reloadWorlds: bool = false
var loadedWorlds = Set.new()

func _ready() -> void:
	load_world()

func _on_new_world_pressed() -> void:
	OpenGenMenu()

func OpenGenMenu():
	$MainButtons.visible = false
	$GenerationPanel.visible = true

func _on_load_world_pressed() -> void:
	$WorldListPanel.visible = true
	$MainButtons.visible = false
	
	if (reloadWorlds):
		load_world()
		reloadWorlds = false

func load_world():
	var directory = DirAccess.open(saveFolderPath)
	var worldList: Array[SimpleGeneratorData] = []

	if directory:
		directory.list_dir_begin()
		var file_name = directory.get_next()
		while file_name != "":
			if not directory.current_is_dir():
				if file_name.ends_with(".tres") and file_name.begins_with("__SIMPLE"):
					var file_path = saveFolderPath + file_name
					var node: SimpleGeneratorData = load(file_path)
					if (not loadedWorlds.has(node)):
						worldList.append(node)
						loadedWorlds.insert(node)
			file_name = directory.get_next()
	else:
		print("An error occurred when trying to access the path.")

	$WorldListPanel.RenderWorldList(worldList)
