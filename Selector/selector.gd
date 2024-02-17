extends Control
class_name SelectorClass

@export var rect_color: Color

@onready var label: Label = $Background/Label
@onready var image: TextureRect = $Background/PanelContainer/TextureRect

@onready var panel_rect: Panel = $Panel
@onready var panel_collider: CollisionShape2D = $Panel/Area2D/CollisionShape2D

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
		panel_rect.position = start_pos
		is_draw = true
	
	if Input.is_action_just_released("RightMouseButton"):
		is_draw = false
		panel_rect.size = Vector2(0, 0)
		panel_collider.shape.size = Vector2.ZERO
		panel_collider.position = Vector2(0, 0)
	
	if is_draw:
		end_pos = get_global_mouse_position()
		
		if start_pos.x > end_pos.x:
			panel_rect.scale.x = -1
		else:
			panel_rect.scale.x = 1
			
		if start_pos.y > end_pos.y:
			panel_rect.scale.y = -1
		else:
			panel_rect.scale.y = 1

		var size = Vector2(abs(start_pos.x - end_pos.x), abs(start_pos.y - end_pos.y))
		panel_rect.size = size
		panel_collider.shape.size = size
		panel_collider.position = Vector2(size.x / 2, size.y / 2)


func _on_area_2d_body_entered(body):
	pass # Replace with function body.


func _on_area_2d_body_exited(body):
	pass # Replace with function body.
