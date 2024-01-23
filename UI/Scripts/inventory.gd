extends Resource
class_name InventoryData

static  var inventory: Dictionary
static var need_to_update_ui: bool = false

static func initialize():
	for i in Slot.slots:
		inventory[i] = null
