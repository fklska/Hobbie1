extends Resource
class_name InventoryItem

@export_category("Base Item")
@export var image: Texture2D
@export var name: String
@export var amount: int
@export var type = Globals.InventoryItemTypes.EMPTY
@export_multiline var description: String

func _init(_texture: Texture2D, _name: String, _amount: int, _type: int = 0, _description = "Description"):
	image = _texture
	name = _name
	amount = _amount
	type = _type
	description = _description

func increase_amount(value: int):
	amount += value
	
func decrease_amount(value: int):
	amount -= value

func get_texture():
	return image

func _compare(other_obj_type: int):
	return type == other_obj_type

func _to_string():
	return ("Name: " + name  + " Amount: " + str(amount) + " Image: " + image.resource_path + " Type: " + str(type))
