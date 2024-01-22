extends Control
class_name Item


var current_texture: Texture2D = null
func set_data(res_texture: Texture2D, amount: int):
	get_node("Image").texture = res_texture
	get_node("Amount").text = str(amount)
	current_texture = res_texture

func get_texture():
	return current_texture
