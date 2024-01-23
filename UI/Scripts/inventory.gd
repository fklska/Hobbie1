extends Resource
class_name InventoryData

@export var inventory: Dictionary
static var need_to_update_ui: bool = false

func _init():
	for i in Slot.slots:
		inventory[i] = null
