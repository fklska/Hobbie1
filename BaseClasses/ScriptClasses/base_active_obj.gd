extends Node2D
class_name ActiveClass

func set_selected():
	get_node("Selector").visible = true
	get_node("Ebutton").visible = true


func hide_selected():
	get_node("Selector").visible = false
	get_node("Ebutton").visible = false
