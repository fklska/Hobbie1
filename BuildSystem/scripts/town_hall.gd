extends Building
class_name TownHall

func action(inventory: Dictionary):
	for slot: Slot in inventory:
		if inventory[slot] != null:
			if inventory[slot] is InventoryItem:
				data.put_in_storage(inventory[slot])
				slot.clear_slot()

func send_obj_data() -> Dictionary:
	return {
		"Description": "Базовое строение в деревне",
		"HP": "%d/%d" % [hp, max_hp]
	}
