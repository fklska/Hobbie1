@tool
class_name DayNightCycle
extends CanvasModulate

signal hour_changed(hour: int)
signal new_day(day: int)
signal night_started(day: int)
signal day_started(day: int)

static var instance: DayNightCycle
static var night := 0.0

@export_range(0.0, 24.0, 0.25, "suffix:h") var start_hour := 8.0
@export_range(0.5, 120.0, 0.5, "suffix:min") var day_length_minutes := 12.0
@export var paused := false
@export_range(0.0, 24.0, 0.25, "suffix:h") var editor_preview_hour := 12.0:
	set(value):
		editor_preview_hour = value
		if Engine.is_editor_hint() and is_inside_tree():
			_apply(value / 24.0)

@export_group("Palette")
@export var ambient: Gradient
@export var night_curve: Curve
@export var cloud_shadow_curve: Curve
@export var fog_curve: Curve

@export_group("Nodes")
@export var sky: SkyOverlay
@export var fireflies: GPUParticles2D

var time_of_day := 0.0
var day := 1
var is_night := false

var hour: float:
	get:
		return time_of_day * 24.0


func _ready() -> void:
	if Engine.is_editor_hint():
		_apply(editor_preview_hour / 24.0)
		return
	instance = self
	time_of_day = start_hour / 24.0
	_apply(time_of_day)
	is_night = night >= 0.5


func _exit_tree() -> void:
	if instance == self:
		instance = null


func _process(delta: float) -> void:
	if Engine.is_editor_hint() or paused:
		return
	var prev_hour := int(hour)
	time_of_day += delta / (day_length_minutes * 60.0)
	if time_of_day >= 1.0:
		time_of_day = fmod(time_of_day, 1.0)
		day += 1
		new_day.emit(day)
	_apply(time_of_day)
	if int(hour) != prev_hour:
		hour_changed.emit(int(hour))
	if is_night != (night >= 0.5):
		is_night = not is_night
		if is_night:
			night_started.emit(day)
		else:
			day_started.emit(day)


func set_hour(value: float) -> void:
	time_of_day = fposmod(value, 24.0) / 24.0
	_apply(time_of_day)


func _apply(t: float) -> void:
	color = ambient.sample(t)
	var n := night_curve.sample_baked(t)
	if Engine.is_editor_hint():
		return
	night = n
	if sky:
		sky.apply(color, n, cloud_shadow_curve.sample_baked(t), fog_curve.sample_baked(t))
	if fireflies:
		fireflies.amount_ratio = n
		if fireflies.emitting != (n > 0.05):
			fireflies.emitting = n > 0.05
