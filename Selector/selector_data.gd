extends Node
class_name SelectorDataClass

static var selected_obj: PhysicsBody2D

static func set_selected_obj(obj: PhysicsBody2D):
	selected_obj = obj
	print_debug("Setted: ", selected_obj)
