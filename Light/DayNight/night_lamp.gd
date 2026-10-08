class_name NightLamp
extends PointLight2D

@export var max_energy := 1.4
@export_range(0.0, 1.0) var flicker := 0.12
@export var flicker_speed := 7.0
@export_range(0.0, 1.0) var turn_on_at := 0.3
@export var random_turn_on := true
@export var fade_speed := 1.5
@export var glow: CanvasItem
@export var show_glow := true

var _level := 0.0
var _phase := 0.0
var _threshold := 0.0


func _ready() -> void:
	_phase = randf() * 100.0
	_threshold = randf_range(0.15, 0.6) if random_turn_on else turn_on_at
	_level = 1.0 if DayNightCycle.night >= _threshold else 0.0
	_update(0.0)


func _process(delta: float) -> void:
	_level = move_toward(_level, 1.0 if DayNightCycle.night >= _threshold else 0.0, delta * fade_speed)
	_update(delta)


func _update(delta: float) -> void:
	_phase += delta * flicker_speed
	var f := 1.0 - flicker * (0.5 + 0.5 * sin(_phase) * sin(_phase * 2.3 + 1.7))
	energy = max_energy * _level * f
	enabled = _level > 0.0
	if glow:
		glow.visible = show_glow and enabled
		glow.modulate.a = _level * f
