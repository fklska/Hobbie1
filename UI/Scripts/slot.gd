extends Panel
class_name Slot

@onready var texture: TextureRect = $Texture
@onready var label_amount: Label = $Amount
const empty_style = Color8(255, 255, 255, 145)
const defualt_style = Color8(255, 255, 255, 255)

func _ready():
	set_empty_style()


func add_item(item_texture: Texture2D, amount: int):
	texture.texture = item_texture
	label_amount.text = str(amount)
	set_default_style()


func clear_slot():
	texture.texture = null
	label_amount.text = ""
	set_empty_style()

func curent_item():
	return [texture, label_amount]

func is_empty():
	if texture.texture != null:
		return false
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

func show_status():
	print_debug(texture.texture)
	print_debug(label_amount.text)
