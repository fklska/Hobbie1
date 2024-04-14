@tool
extends Node

func _ready():
	var data = AllInventoryObjects.ALL_ITEMS
	print_debug(data)
	for key in data:
		print_debug(key)
		var scene: PackedScene = load(data[key])
		var obj:ItemNode = scene.instantiate()
		print_debug(obj.ITEM.description)
		
