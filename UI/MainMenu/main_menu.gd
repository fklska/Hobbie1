@tool
extends Control
class_name MainMenu

@export_dir var saveFolderPath: String = "res://SavedWorlds/"

func _on_new_world_pressed() -> void:
	OpenGenMenu()

func OpenGenMenu():
	$VBoxContainer.visible = false
	$GenerationPanel.visible = true

func _on_load_world_pressed() -> void:
	load_world()

func load_world():
	var directory = DirAccess.open(saveFolderPath)
	var worldList: Array[WorldScene] = []

	if directory:
		directory.list_dir_begin()
		var file_name = directory.get_next()
		while file_name != "":
			if not directory.current_is_dir():
				if file_name.ends_with(".tscn"):
					var file_path = saveFolderPath + file_name
					print("Found file: ", file_path)
					var node: PackedScene = load(file_path)
					worldList.append(node.instantiate())
			file_name = directory.get_next()
	else:
		print("An error occurred when trying to access the path.")

	print_debug(worldList)
	$WorldListPanel.RenderWorldList(worldList)
	$WorldListPanel.visible = true
