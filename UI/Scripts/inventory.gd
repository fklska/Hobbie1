extends Resource
class_name InventoryData

static  var inventory: Dictionary
static var need_to_update_ui: bool = false

static func initialize():
	for i in Slot.slots:
		inventory[i] = null

static func add_item(slot: Slot, item: InventoryItem):
	inventory[slot] = item
	#print_debug("Add: ", inventory)

static func remove_item_from_slot(slot: Slot):
	inventory[slot] = null
	#print_debug("Take: ", inventory)
