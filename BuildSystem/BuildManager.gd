extends TabBar

@onready var buildMenu: Menu = $".."
var TownHallPrefab = preload("res://BuildSystem/buildings/TownHall.tscn")

func _on_blacksmith_butt_button_down():
	pass # Replace with function body.


func _on_town_hall_butt_button_down():
	#buildMenu.close()
	var TownHall = TownHallPrefab.instantiate()
	get_tree().root.add_child(TownHall)
	print_debug(TownHall.position)
	TownHall.global_position = get_global_mouse_position()
	TownHall.start_build()

func _on_fire_butt_button_down():
	pass # Replace with function body.
