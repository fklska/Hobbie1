extends Node2D

@export var noise: FastNoiseLite
@export var SIZE: Vector2i

@export var water_height: float
@export var sand_height: float

@onready var dirt: TileMapLayer = $Dirt
@onready var grass: TileMapLayer = $Grass


var sand_tiles = []
var grass_tiles = []
var water_tiles = []

func generate():
	noise.seed = randi()

	for x in range(-SIZE.x / 2, SIZE.x / 2):
		for y in range(-SIZE.y / 2, SIZE.y / 2):
			
			var height = noise.get_noise_2d(x, y)
			
			if height < water_height:
				water_tiles.append(Vector2i(x, y))
				
			elif height < sand_height:
				sand_tiles.append(Vector2i(x, y))
			else:	
				grass_tiles.append(Vector2i(x, y))
	
	
