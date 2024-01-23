extends Control
class_name InventoryUI

@export var inv: InventoryData

func update_slots():
	for i: Slot in inv.inventory:
		if inv.inventory[i] != null:
			i.update(inv.inventory[i])
