@tool
extends Node2D

@export var map_size: Vector2i
var noise: FastNoiseLite = load("res://ProceduralGeneration/biome_noise.tres")
var map = []
var root_node = Node2D.new()

func _ready() -> void:
	pass
	
func load_noise(path="res://ProceduralGeneration/biome_noise.tres"):
	var noise = load(path)
	return noise

func initialize_map(size: Vector2i):
	map.resize(map_size.y)
	for x in range(map_size.y):
		var dummy = []
		dummy.resize(map_size.x)
		map[x] = dummy

	print_debug(map)
