extends Panel
class_name Slot

@onready var texture: TextureRect = $Item/Texture
@onready var label_amount: Label = $Item/Amount
const empty_style = Color8(255, 255, 255, 145)
const defualt_style = Color8(255, 255, 255, 255)
const selected_color = Color8(255, 255, 145, 255)

enum {
	EMPTY,
	FILL,
	SELECTED
}
var state = EMPTY

func _process(delta):
	state_machine()


func state_machine():
	match state:
		EMPTY:
			set_empty_style()
		FILL:
			set_default_style()
		SELECTED:
			set_selected_style()

func add_item(item_texture: Texture2D, amount: int):
	texture.texture = item_texture
	label_amount.text = str(amount)
	state = FILL


func clear_slot():
	texture.texture = null
	label_amount.text = ""
	state = EMPTY

func curent_item():
	return [texture, label_amount]

func is_empty():
	if texture.texture != null:
		state = FILL
		return false

	state = EMPTY
	return true

func update_info(new_amount: int):
	label_amount.text = str(new_amount)

	if label_amount.text == "1":
		label_amount.visible = false
	else: 
		label_amount.visible = true

func set_empty_style():
	modulate = empty_style
	
func set_default_style():
	modulate = defualt_style

func set_selected_style():
	modulate = selected_color

func show_status():
	print_debug(texture.texture)
	print_debug(label_amount.text)


var InArea: bool = false
func _on_mouse_entered():
	state = SELECTED
	

func _on_mouse_exited():
	if is_empty():
		state = EMPTY
		return null
	state = FILL


func _on_gui_input(event: InputEvent):
	if InArea:
		if event.is_action_pressed("attack"):  # Если держу -> перетаскиваю предмет
			print_debug("Pressed") # Как только отпустил (проверка) -> засунь предмет в этот слот State должен быть SELECTED
