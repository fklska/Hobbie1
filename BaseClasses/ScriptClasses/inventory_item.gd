extends Resource
class_name InventoryItem

@export_category("Base Item")
@export var image: Texture2D
@export var name: String
@export var amount: int
@export_enum("Resourse", "Wood", "Stone", "Iron", "Gold", "Weapon") var type: String

func _init(_texture: Texture2D, _name: String, _amount: int, _type: String = "Resource"):
	image = _texture
	name = _name
	amount = _amount
	type = _type

func increase_amount(value: int):
	amount += value
	
func decrease_amount(value: int):
	amount += value
	
func _compare(other_obj_type: String):
	return type == other_obj_type

func _to_string():
	return ("Name: " + name  + " Amount: " + str(amount) + " Image: " + image.resource_path + " Type: " + type)
