extends CanvasLayer
class_name UI

@onready var player: Player = $"../Player"

var slots
var InventorySlots: Dictionary = {}
func _ready():
	slots = get_node("MarginContainer/BoxContainer").get_children()


func update_ui(Inventory: Dictionary):
	for obj: Texture2D in Inventory:
		if InventorySlots.has(obj):
			InventorySlots[obj].update_info(Inventory[obj])
		else:
			for slot: Slot in slots:
				if slot.is_empty():
					slot.add_item(obj, Inventory[obj])
					InventorySlots[obj] = slot
					break
			print_debug("Inventory full")
		
		
