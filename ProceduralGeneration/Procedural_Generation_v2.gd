@tool
extends Node2D

var map = [[null]]
@export var map_size: Vector2i = Vector2i(1280, 720)
@export var noise: FastNoiseLite
@onready var texture: Sprite2D = $Sprite2D



func _ready() -> void:
	generate()
	texture.texture.noise = noise

func generate():
	noise.seed = randi()
