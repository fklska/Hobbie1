extends Node2D
class_name ActiveClass

@export var data: BaseDataClass
var active_func: Callable = active

func active():
	pass
	

func set_selected():
	get_node("Selector").visible = true
	get_node("Ebutton").visible = true


func hide_selected():
	get_node("Selector").visible = false
	get_node("Ebutton").visible = false
