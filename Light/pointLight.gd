@tool
extends PointLight2D

var noise
var time: float = 0.0
@export var SPEED: Vector3 = Vector3(-20, 0, 0)
@export var world_scale: float = 1.0
func _ready() -> void:
	if is_instance_of(texture, NoiseTexture2D):
		noise = texture.noise

func _process(delta):
	if not noise:
		return

	noise.offset += SPEED * delta
