extends Control

@export var inv: InventoryData


func update_slots():
	for i: Slot in inv.inventory:
		i.update(inv.inventory[i])
