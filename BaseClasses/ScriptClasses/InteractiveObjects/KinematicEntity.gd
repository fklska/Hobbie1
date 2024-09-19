extends CharacterBody2D
class_name KinematicBodyEntity

@onready var anim: AnimatedSprite2D = $AnimatedSprite2D

var shader: ShaderMaterial

func _ready():
	shader = anim.material
	if shader == null:
		print_debug("SHADER INSTALL")

func set_outline():
	shader.set_shader_parameter("enable", true)
	
func hide_outline():
	shader.set_shader_parameter("enable", false)

func get_texture():
	pass

func send_obj_data() -> Dictionary:
	return {
		"Basic": "Basic KinematicBody2dObject",
		"2nd": "Second label"
	}

func _on_mouse_entered():
	set_outline()

func _on_mouse_exited():
	if SelectorDataClass.selected_obj != self:
		hide_outline()

func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("LeftMouseButton"):
		SelectorDataClass.set_selected_obj(self)

