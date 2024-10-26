extends Node2D

func _ready():
	var dic1 = {"test": 5}
	var dic2 = {
		"test2": 14,
		"test3": 19
	}
	dic1.merge(dic2)
	
	print_debug(dic1)
