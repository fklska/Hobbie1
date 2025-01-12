extends Node2D

var new_rotation
var tween
#@export var res: Resource
var prefab = load("res://balvanka.tscn")
var item = null
var saved_res_data = null

func _ready():
	pass

func _input(event: InputEvent):
	if event.is_action_pressed("test"):
		item.res.value1 += 1
		ResourceSaver.save(item.res)
		print_debug(item.res.value1)

	if event.is_action_pressed("LeftMouseButton"):
		item = prefab.instantiate()
		print_debug(item.res.value1)
		add_child(item)
		saved_res_data = item.res
	
	if event.is_action_pressed("RightMouseButton"):
		if item:
			item.queue_free()
			item = null
