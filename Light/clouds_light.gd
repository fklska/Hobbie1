@tool
extends PointLight2D

@export var noise: FastNoiseLite
var pixels: PackedByteArray
var size: int = 32
@onready var player_main_character: CharacterBody2D = $"../Player_MainCharacter"
var shader: ShaderMaterial

func _ready() -> void:
	noise = texture.noise
	shader = material
	
func _process(delta: float) -> void:
	noise.offset += delta * Vector3(-20, 0, 0)
	shader.set_shader_parameter("player_pos", player_main_character.global_position)
	#queue_redraw()
