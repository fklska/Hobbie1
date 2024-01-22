extends Control
class_name Item

func set_data(res_texture: Texture2D, amount: int):
	get_node("Image").texture = res_texture
	get_node("Amount").text = str(amount)
	
