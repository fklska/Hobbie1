extends Range

const POP_TIME := 0.2

@export var color := Color(1.0, 0.8, 0.32)

var _pop := 0.0


func _ready() -> void:
	value_changed.connect(_on_value_changed)
	set_process(false)


func _on_value_changed(new_value: float) -> void:
	if new_value >= max_value:
		_pop = POP_TIME
		set_process(true)
	queue_redraw()


func _process(delta: float) -> void:
	_pop -= delta
	set_process(_pop > 0.0)
	queue_redraw()


func _draw() -> void:
	var center := size / 2
	var radius := minf(size.x, size.y) / 2
	if _pop > 0.0:
		var t := _pop / POP_TIME
		draw_circle(center, radius * (1.4 - 0.4 * t), Color(color, t), true, -1.0, true)
	if ratio > 0.0:
		draw_circle(center, radius, Color(color.darkened(0.7), 0.7), true, -1.0, true)
		draw_arc(center, radius / 2, -PI / 2, -PI / 2 + TAU * ratio, 48, color, radius, true)
