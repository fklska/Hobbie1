extends Control
class_name MainMenu

var path = "user://new_world1.save"
var generator = preload("res://Generator.tscn")

func save_world(world):
	var file = FileAccess.open(path, FileAccess.WRITE)
	file.store_var(world, true)

func load_world():
	pass


func _on_new_world_pressed() -> void:
	var map = generator.instantiate()
	map.generate()
	save_world(map)
