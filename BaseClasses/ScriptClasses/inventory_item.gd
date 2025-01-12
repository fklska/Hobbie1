extends Area2D
class_name InventoryItem

@export_category("Base Item")
@export var image: Texture2D: 
	set(value):
		image = value
		if is_instance_valid(textureRect):
			_update_texture(value)

@export var amount: int
@export var type = Globals.InventoryItemTypes.EMPTY
@export_multiline var description: String
var parametrs: Dictionary = {
	# Player
	# Player_Agility
}

@onready var textureRect: TextureRect = $Texture

func setParametr(key, value):
	parametrs[key] = value

func isConsumable():
	return true

func DoFunction(params={}):
	pass

func set_selected(args: Dictionary):
	pass

func increase_amount(value: int):
	amount += value

func decrease_amount(value: int):
	amount -= value

func get_texture():
	return image

func _compare(other_obj_type: int):
	return type == other_obj_type

func _update_texture(value):
	if value != null:
		textureRect.texture = value
	else:
		textureRect.texture = null

func set_properties(_texture: Texture2D, _amount: int, _type: int):
	image = _texture
	amount = _amount
	type = _type

func _to_string():
	return ("Name: " + name  + " Amount: " + str(amount) + " Image: " + image.resource_path + " Type: " + str(type))
