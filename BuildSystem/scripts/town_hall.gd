extends StaticBodySelectedObject
class_name Building

@export var data: StorageDataClass
@export var storage_ui: StorageUI

func action(inventory: Dictionary):
	for slot: Slot in inventory:
		if inventory[slot] != null:
			if inventory[slot] is InventoryItem:
				data.put_in_storage(inventory[slot])
				slot.clear_slot()
			
	storage_ui.update_ui(data.data_items)

func send_obj_data() -> Dictionary:
	return {
		"Description": "Базовое строение в деревне"
	}

func create_human():
	print_debug("Villager Created!")
