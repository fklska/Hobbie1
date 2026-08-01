@tool
extends CanvasModulate

@export var colors: GradientTexture1D
const MINUTES_PER_DAY = 1440
const MINUTES_PER_HOUR = 60
const INGAME_TO_REAL_MINUTE_DURATION = (2 * PI) / MINUTES_PER_DAY
@export var INGAME_SPEED = 20.0
@export var INITIAL_HOUR = 12

@export var clouds: PrettyClouds
@export var bloom_light: PointLight2D
@export var time = 0
@export var curve: Curve

const cloud_conts = 0.75
var day_count = 1

func _ready() -> void:
	time = INGAME_TO_REAL_MINUTE_DURATION * MINUTES_PER_HOUR * INITIAL_HOUR
	
func _process(delta: float) -> void:
	time += delta * INGAME_TO_REAL_MINUTE_DURATION * INGAME_SPEED
	var value = (sin(time) + 1) / 2
	self.color = colors.gradient.sample(value)
	#clouds.modulate = self.color
	bloom_light.color = self.color
	if value < 0.5:
		bloom_light.energy = 3.5 * curve.sample(value)
	else:
		bloom_light.energy = value * 2
	clouds.texture.color_ramp.set_offset(1, max(0.2, min(value, cloud_conts)))
