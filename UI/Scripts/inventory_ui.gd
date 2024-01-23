extends Control
class_name InventoryUI


func update_slots():
	for i: Slot in InventoryData.inventory:
		if InventoryData.inventory[i] != null:
			i.update(InventoryData.inventory[i])
	print_debug(InventoryData.inventory)

func _on_gui_input(event: InputEvent):
	if event.is_action_pressed("inventory"):
		visible = !visible
