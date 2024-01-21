extends MarginContainer
class_name Slot

@onready var texture: TextureRect = $SlotImage/TextureRect
@onready var label_amount: Label = $SlotImage/Amount

func add_item(item_texture: Texture2D, amount: int):
	texture.texture = item_texture
	label_amount.text = str(amount)
	
func clear_slot():
	texture.texture = null
	label_amount.text = ""

func curent_item():
	return [texture, label_amount]

func is_empty():
	if texture.texture != null:
		return false
	return true

func update_info(new_amount: int):
	label_amount.text = str(new_amount)

func show_status():
	print_debug(texture.texture)
	print_debug(label_amount.text)
