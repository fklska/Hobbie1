extends Control
class_name SelectorClass

@export var panel_rect: Panel
@export var rect_color: Color

@onready var label: Label = $Background/Label
@onready var image: TextureRect = $Background/PanelContainer/TextureRect

static var selected_object: Array

var start_pos: Vector2
var end_pos: Vector2

var is_draw: bool = false

var prev_selected

func _process(delta):
	#if prev_selected != selected_object:
		#update_selector()
	draw_selector_rect()

func update_selector():
	if selected_object != null:
		if selected_object.size() == 1:
			var data: Dictionary = selected_object[0].show_selected_info()
			image.texture = data.get("texture")
			label.text = data.get("text", "No data")
		else:
			label.text = "Selected: " + str(selected_object.size()) + "objects"
		prev_selected = selected_object
func clear():
	selected_object.clear()


func draw_selector_rect():
	#print_debug("Test")
	if Input.is_action_just_pressed("RightMouseButton"):
		clear()
		start_pos = get_global_mouse_position()
		panel_rect.global_position = start_pos
		is_draw = true
	
	if Input.is_action_just_released("RightMouseButton"):
		is_draw = false
	
	if is_draw:
		end_pos = get_global_mouse_position()
		var size = Vector2(start_pos.x - end_pos.x, start_pos.y - end_pos.y)
		panel_rect.size = size
