@tool
extends Area2D
class_name ItemNode

@onready var textureRect: TextureRect = $Texture
@export var ITEM: InventoryItem: set = _set_item

func _set_item(value: InventoryItem):
	ITEM = value
	if is_instance_valid(textureRect):
		_update_properties(value)

func _update_properties(value):
	if value != null:
		textureRect.texture = ITEM.image
	else:
		textureRect.texture = null
	
func get_amount():
	return ITEM.amount
