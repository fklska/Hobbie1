extends Panel
class_name Slot

@export var current_item: InventoryItem

@onready var item_sprite: TextureRect = $CenterContainer/item_display
@onready var item_amount: Label = $CenterContainer/item_display/item_amount

static var flying_obj: InventoryItem = null

const empty_style = Color8(255, 255, 255, 215)
const defualt_style = Color8(255, 255, 255, 255)
const selected_color = Color8(255, 255, 145, 255)

enum {
	EMPTY,
	FILL,
	SELECTED
}

var state = EMPTY
var slot_number_label: Label


func _process(_delta):
	state_machine()
	

func _ready():
	slot_number_label = get_node("Label")
	
func update(item: InventoryItem):
	current_item = item
	item_amount.text = str(item.amount)
	item_sprite.texture = item.image

	refresh_state()

func clear_slot():
	current_item = null
	item_amount.text = ""
	item_sprite.texture = null

	refresh_state()

func state_machine():
	match state:
		EMPTY:
			set_empty_style()
		FILL:
			set_default_style()
		SELECTED:
			set_selected_style()

func refresh_state():
	if is_empty():
		state = EMPTY
	elif state == SELECTED:
		state = SELECTED
	else:
		state = FILL

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

func _on_gui_input(event: InputEvent):
	if event.is_action_pressed("RightMouseButton"):
		print_debug(self)
		
	if event.is_action_released("LeftMouseButton"):
		if flying_obj == null:  # Take Item
			flying_obj = current_item
			clear_slot()
		else:  # Put Item
			if is_empty():
				current_item = flying_obj
				update(flying_obj)
				flying_obj = null
				InventoryUI.disable_display = true
			else:
				if current_item._compare(flying_obj.type):
					print_debug("Stack")
				else:
					print_debug("Slot zanyat!")
