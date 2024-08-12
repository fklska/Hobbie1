extends Area2D
class_name ItemNode

@export var textureRect: TextureRect 
@export var ITEM: InventoryItem: set = _set_item


func _set_item(value: InventoryItem):
	ITEM = value
	#if not is_instance_valid(textureRect):
	#	return
	if value != null:
		textureRect.texture = ITEM.image
	else:
		textureRect.texture = null
	
	
func get_amount():
	return ITEM.amount
