extends StaticBody2D
class_name StaticBodySelectedObject

@onready var texture: Sprite2D = $Texture

var shader: ShaderMaterial

func _ready():
	shader = texture.material

func set_outline():
	shader.set_shader_parameter("enable", true)
	
func hide_outline():
	shader.set_shader_parameter("enable", false)

func _on_mouse_entered():
	set_outline()


func _on_mouse_exited():
	hide_outline()


func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("LeftMouseButton"):
		print_debug("InputEvenet")
		SelectorDataClass.set_selected_obj(self)
