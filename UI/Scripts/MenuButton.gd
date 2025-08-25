extends Button

@onready var menu: Menu = $"../BuildMenu"

func _on_pressed():
	menu.visible = !menu.visible
	BuildMode.buildMode = true
