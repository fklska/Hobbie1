extends ActiveClass
class_name Building

@export var data: StorageDataClass
@onready var storage_ui: StorageUI = $CanvasLayer2/storage_ui

func action(inventory: Dictionary):
	for slot: Slot in inventory:
		if inventory[slot] != null:
			data.add_item(inventory[slot])
			InventoryData.remove_item_from_slot(slot)
			slot.clear_slot()
			
	storage_ui.update_ui(data.data_items)
