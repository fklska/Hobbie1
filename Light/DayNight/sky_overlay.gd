class_name SkyOverlay
extends Node2D

var _rect := Rect2()
var _time := 0.0


func _process(delta: float) -> void:
	_time += delta
	var inv := get_viewport().get_canvas_transform().affine_inverse()
	var px := inv.get_scale().x
	_rect = (inv * get_viewport_rect()).grow(64.0 * px)
	material.set_shader_parameter("time", _time)
	material.set_shader_parameter("world_per_screen_px", px)
	queue_redraw()


func _draw() -> void:
	draw_rect(_rect, Color.WHITE)


func apply(ambient: Color, night: float, cloud_shadow: float, fog: float) -> void:
	var lin := ambient.srgb_to_linear()
	material.set_shader_parameter("ambient", Vector3(lin.r, lin.g, lin.b))
	material.set_shader_parameter("night", night)
	material.set_shader_parameter("shadow_strength", cloud_shadow)
	material.set_shader_parameter("fog_amount", fog)
