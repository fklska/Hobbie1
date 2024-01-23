extends Resource
class_name InventoryData

@export var inventory: Dictionary

func _init():
	for i in Slot.slots:
		inventory[i] = null
