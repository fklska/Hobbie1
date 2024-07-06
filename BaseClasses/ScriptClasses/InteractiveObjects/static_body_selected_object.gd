extends StaticBody2D
class_name StaticBodySelectedObject

@onready var texture: Sprite2D = $Texture

var shader: ShaderMaterial

func _ready():
	shader = texture.material

func get_texture():
	return texture

func set_outline():
	shader.set_shader_parameter("enable", true)
	
func hide_outline():
	shader.set_shader_parameter("enable", false)

func _on_mouse_entered():
	MouseInfoPanel.setPosSignal()
	set_outline()

func _on_mouse_exited():
	if SelectorDataClass.selected_obj != self:
		hide_outline()

func send_obj_data() -> Dictionary:
	return {
		"Basic": "Basic staticBody2dObject",
		"2nd": "Second label"
	}

func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("LeftMouseButton"):
		SelectorDataClass.set_selected_obj(self)
