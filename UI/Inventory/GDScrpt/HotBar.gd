extends MarginContainer
class_name HotBarClass

@export var slot_selector: TextureRect

@export var test_weapon: PackedScene

static var current_selected_slot: Slot

signal selected_slot_changed(item: InventoryItem)

var hotbar_slots: Array
var key_slot_map: Dictionary = {}

func _ready():
	hotbar_slots = $HBoxContainer.get_children()
	initialize_key_slot_map(hotbar_slots)
	current_selected_slot = hotbar_slots[0]

func _input(event: InputEvent):
	if event.is_action_pressed("HotBar"):
		var slot = key_slot_map[event.as_text()]
		current_selected_slot = slot
		set_slot_selector(slot)

func set_slot_selector(slot: Slot):
	slot_selector.reparent(slot)
	slot_selector.position = Vector2(0, 0)
	selected_slot_changed.emit(current_selected_slot.CurrentItem)

func initialize_key_slot_map(slots: Array):
	var index = 1
	
	for i in slots:
		key_slot_map[str(index)] = i
		index += 1
