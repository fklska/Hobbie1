extends Button

@export var menu: Menu

func _on_pressed():
	menu.visible = !menu.visible
