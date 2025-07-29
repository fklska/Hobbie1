extends Button

@onready var menu: Menu = $"../Menu"

func _on_pressed():
	menu.visible = !menu.visible
