
extends CanvasModulate

@export var colors: GradientTexture1D
const MINUTES_PER_DAY = 1440
const MINUTES_PER_HOUR = 60
const INGAME_TO_REAL_MINUTE_DURATION = (2 * PI) / MINUTES_PER_DAY
@export var INGAME_SPEED = 20.0
@export var INITIAL_HOUR = 12

@export var light: PointLight2D

@export var time = 0

func _ready() -> void:
	time = INGAME_TO_REAL_MINUTE_DURATION * MINUTES_PER_HOUR * INITIAL_HOUR
	
func _process(delta: float) -> void:
	time += delta * INGAME_TO_REAL_MINUTE_DURATION * INGAME_SPEED
	var value = (sin(time) + 1) / 2
	self.color = colors.gradient.sample(value)
	light.color = self.color
