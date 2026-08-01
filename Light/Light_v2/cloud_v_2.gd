@tool
extends Sprite2D
class_name PrettyClouds

var player
var noise: FastNoiseLite
var shader: ShaderMaterial

func _ready() -> void:
	player = get_node_or_null("../Player_MainCharacter")
	noise = texture.noise
	shader = material


func _process(delta: float) -> void:
	noise.offset += delta * Vector3(-2, 0, 0)
	shader.set_shader_parameter("player_pos", player.global_position if player else Vector2(0, 0))
