@tool
extends PointLight2D

var noise
@export var SPEED: Vector3 = Vector3(-50, 0, 0)
func _ready() -> void:
	if is_instance_of(texture, NoiseTexture2D):
		noise = texture.noise

func _process(delta: float) -> void:
	noise.offset += delta * SPEED
