extends Button

@onready var menu = $"../BuildMenu"

func _on_pressed():
	menu.visible = !menu.visible
	#BuildMode.buildMode = true
