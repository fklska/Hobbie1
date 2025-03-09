extends ActiveResourses
class_name WoodClass

func _ready() -> void:
	shader = texture.material
	texture.material.set_shader_parameter("random_offset", randf() * 10.0)
	texture.material.set_shader_parameter("random_speed", randf())
