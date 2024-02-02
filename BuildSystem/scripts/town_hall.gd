extends ActiveClass
class_name Building

@export var data: StorageDataClass
@onready var storage_ui: StorageUI = $CanvasLayer2/storage_ui

func action(inventory: Dictionary):
	for slot: Slot in inventory:
		if inventory[slot] != null:
			if inventory[slot] is InventoryItem:
				data.put_in_storage(inventory[slot])
				InventoryData.remove_item_from_slot(slot)
				slot.clear_slot()
			
	storage_ui.update_ui(data.data_items)

func show_selected_info():
	return {
		"texture": get_node("Sprite2D").texture,
		"text": "Building"
		}

func _on_mouse_entered():
	if Input.is_action_pressed("LeftMouseButton"):
		SelectorClass.selected_object = self
