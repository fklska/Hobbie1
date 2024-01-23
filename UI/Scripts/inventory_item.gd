extends Resource
class_name InventoryItem

@export var image: Texture2D
@export var name: String
@export var amount: int

func _init(_texture: Texture2D, _name: String, _amount: int):
	image = _texture
	name = _name
	amount = _amount

func _to_string():
	return ("Name: " + name + "\n" + "Amount: " + str(amount) + "\n" + "Image: " + image.resource_path)
	
