extends Panel

@onready var panel_rect: Panel = $"."
@onready var panel_collider: CollisionShape2D = $Area2D/CollisionShape2D

static var selected_object: Array

var start_pos: Vector2
var end_pos: Vector2

var is_draw: bool = false

func _process(delta):
	draw_selector_rect()

func clear():
	selected_object.clear()


func draw_selector_rect():
	if Input.is_action_just_pressed("RightMouseButton"):
		clear()
		start_pos = get_global_mouse_position()
		panel_rect.global_position = start_pos
		is_draw = true
	
	if Input.is_action_just_released("RightMouseButton"):
		is_draw = false
		panel_rect.size = Vector2(0, 0)
		panel_collider.shape.size = Vector2.ZERO
		panel_collider.global_position = Vector2(0, 0)
	
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
	selected_object.append(body)

func _on_area_2d_body_exited(body):
	selected_object.erase(body)
