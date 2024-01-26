extends ActiveClass
class_name Building

@onready var storage_ui: StorageUI = $CanvasLayer/storage_ui
@export var data: StorageDataClass

func action(inventory: Dictionary):
	for slot in inventory:
		if inventory[slot] != null:
			data.add_item(inventory[slot])
			
	storage_ui.update_ui(data.data_items)
