extends StaticBody2D
class_name StaticBodySelectedObject

@export_category("SelectedObject")
@export var Name: String
@export var Type: String
@export var Description: String

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
	MouseInfoPanel.set_show(global_position + Vector2(32, 0))
	set_outline()

func _on_mouse_exited():
	MouseInfoPanel.hide()
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
