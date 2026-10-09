extends PanelContainer
class_name HotBarClass

static var current_selected_slot: Slot

signal selected_slot_changed(item: InventoryItem)

@onready var slots: Array = %Slots.get_children()


func _ready() -> void:
	select(0)


func _unhandled_input(event: InputEvent) -> void:
	if not event is InputEventKey or not event.is_action_pressed("HotBar"):
		return
	var key: int = event.physical_keycode if event.physical_keycode != KEY_NONE else event.keycode
	var index := key - KEY_1
	if index >= 0 and index < slots.size():
		select(index)


func select(index: int) -> void:
	for i in slots.size():
		slots[i].Selected = i == index
	current_selected_slot = slots[index]
	selected_slot_changed.emit(current_selected_slot.CurrentItem)
