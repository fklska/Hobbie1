extends CanvasLayer
class_name UI

var slots

var null_item = preload("res://UI/ui_item.tscn")

func _ready():
	slots = get_node("HBoxContainer").get_children()


func update_ui(Inventory: Dictionary):
	var slots_inventory = Slot.Inventory
	for obj: Texture2D in Inventory:
		if slots_inventory.has(obj):
			slots_inventory[obj].update_info(Inventory[obj])
		else:
			for slot: Slot in slots:
				if slot.is_empty():
					var item: Item = null_item.instantiate()
					item.set_data(obj, Inventory[obj])
					slot.add_item(item)
					break
