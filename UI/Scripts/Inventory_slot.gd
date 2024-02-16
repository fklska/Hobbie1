extends Slot
class_name InventorySlot


static var slot_amount = 1
static var slots: Array[InventorySlot]

var current_slot_number = 0

func _init():
	current_slot_number = slot_amount
	slot_amount += 1
	slots.append(self)

func _ready():
	slot_number_label = get_node("Label")
	slot_number_label.text = str(current_slot_number)
	InventoryData.initialize()

func _to_string():
	return ("InventorySlot #" + str(current_slot_number))
