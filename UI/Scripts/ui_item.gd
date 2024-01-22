extends Control
class_name Item

func set_data(texture: Texture2D, amount: int):
	get_node("Texture").texture = texture
	get_node("Amount").text = str(amount)
	
