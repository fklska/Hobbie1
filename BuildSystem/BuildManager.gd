extends TabBar

@onready var buildMenu: Menu = $".."
var TownHallPrefab = preload("res://BuildSystem/buildings/TownHall.tscn")

func _on_blacksmith_butt_button_down():
	pass # Replace with function body.


func _on_town_hall_butt_button_down():
	buildMenu.close()
	var bmode = get_node("/root/BuildMode") as BuildModeManager
	bmode.build_mode = true
	print_debug(bmode.position)
	print_debug(bmode.global_position)
	var TownHall = TownHallPrefab.instantiate()
	get_tree().root.add_child(TownHall)
	TownHall.global_position = get_global_mouse_position()
	TownHall.start_build()

func _on_fire_butt_button_down():
	pass # Replace with function body.
