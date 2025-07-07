extends Control
class_name MainMenu

func save_world(world):
	pass

func load_world():
	pass

func _on_new_world_pressed() -> void:
	GENERATOR.Generate()
