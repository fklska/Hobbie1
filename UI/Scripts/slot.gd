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
static var Inventory: Dictionary = {}
var current_slot_number = 0
var current_item: Item = null

func _init():
	current_slot_number = slot_amount
	slot_amount += 1
	
func _process(delta):
	state_machine()
	#if Input.is_action_just_pressed("attack"):
	#	if state == SELECTED:
	#		show_status()
	
	if flying_obj != null:
		flying_obj.get_node(".").global_position = get_global_mouse_position()
	
	

func _ready():
	slot_number_label.text = str(current_slot_number)
	current_item = $Item
	state = FILL
	
func state_machine():
	match state:
		EMPTY:
			set_empty_style()
		FILL:
			set_default_style()
		SELECTED:
			set_selected_style()

func add_item(item: Item):
	add_child(item)
	current_item = item
	state = FILL
	Inventory[item.current_texture] = get_node(".")

func clear_slot():
	remove_child(get_node("Item"))
	Inventory.erase(current_item.current_texture)
	current_item = null
	state = EMPTY

func get_curr_item():
	return current_item

func is_empty():
	if current_item != null:
		state = FILL
		return false
	state = EMPTY
	return true

func update_info(new_amount: int):
	var item_label: Label = current_item.get_child(1)
	item_label.text = str(new_amount)

	if item_label.text == "1":
		item_label.visible = false
	else: 
		item_label.visible = true

func set_empty_style():
	modulate = empty_style
	
func set_default_style():
	modulate = defualt_style

func set_selected_style():
	modulate = selected_color

func show_status():
	print_debug("Item: ", current_item)
	print_debug("flyingobj = ", flying_obj)
	print_debug("slotnumber = ", current_slot_number)


func _on_mouse_entered():
	state = SELECTED
	

func _on_mouse_exited():
	if is_empty():
		state = EMPTY
		return null
	state = FILL

func drag():
	pass # Убираю flying obj из слота
	
func drop():
	pass # Ставлю flying obj в слот


func _on_gui_input(event: InputEvent):
	if event.is_action_pressed("attack"):
		if state == SELECTED and not is_empty():
			flying_obj = current_item.get_node(".")
			print_debug(flying_obj.get_tree())
			print_debug(flying_obj.get_node(".").position)

		#print_debug("Global: ", get_global_mouse_position())
		#print_debug("Local: ", get_local_mouse_position()) # Replace with function body.
