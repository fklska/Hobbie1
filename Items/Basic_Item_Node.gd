@tool
extends Sprite2D
class_name ItemNode

@export var ITEM: InventoryItem: set = _set_item


func _set_item(value: InventoryItem):
	ITEM = value
	if ITEM != null:
		self.texture = ITEM.image


func get_amount():
	return ITEM.amount
