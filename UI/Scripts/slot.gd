extends Panel
class_name Slot

@onready var slot_number_label: Label = $SlotNumber

const empty_style = Color8(255, 255, 255, 145)
const defualt_style = Color8(255, 255, 255, 255)
const selected_color = Color8(255, 255, 145, 255)

enum {
	EMPTY,
	FILL,
	SELECTED
}
var state = EMPTY

static var flying_obj: Control = null
static var slot_amount = 0
static var slots: Array[Slot]

var current_slot_number = 0
var current_item: InventoryItem = null

@onready var item_sprite: Sprite2D = $CenterContainer/item_display
@onready var item_amount: Label = $Label

func _init():
	current_slot_number = slot_amount
	slot_amount += 1
	slots.append(self)
	
func _process(delta):
	state_machine()
	

func _ready():
	slot_number_label.text = str(current_slot_number)
	
func update(item: InventoryItem):
	current_item = item
	item_amount.text = str(item.amount)
	item_sprite.texture = item.image

	refresh_state()
	
func refresh_state():
	if is_empty():
		state = EMPTY
	elif state == SELECTED:
		state = SELECTED
	else:
		state = FILL
	
func state_machine():
	match state:
		EMPTY:
			set_empty_style()
		FILL:
			set_default_style()
		SELECTED:
			set_selected_style()

func is_empty():
	if current_item == null:
		return true
	return false

func set_empty_style():
	modulate = empty_style
	
func set_default_style():
	modulate = defualt_style

func set_selected_style():
	modulate = selected_color

func _on_mouse_entered():
	state = SELECTED

func _on_mouse_exited():
	if is_empty():
		state = EMPTY
		return null
	state = FILL

func _to_string():
	return "Slot #: " + current_slot_number
