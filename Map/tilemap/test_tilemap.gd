@tool
extends Node2D

@export var noise: FastNoiseLite
@export var SIZE: Vector2i

@export var water_height: float
@export var dirt_height: float
@export var grass_height: float

@onready var dirt: TileMapLayer = $Dirt
@onready var grass: TileMapLayer = $Grass
@onready var water: TileMapLayer = $Water

@onready var texture_rect: TextureRect = $TextureRect

var dirt_tiles = []
var grass_tiles = []
var water_tiles = []

var height_map = []
var offsets 

func _ready() -> void:
	offsets = PackedFloat32Array([water_height, dirt_height])
	generate()
	texture_rect.texture.noise = noise
	#texture_rect.texture.color_ramp.offsets = offsets

func generate():
	clear()
	
	noise.seed = randi()

	for x in range(SIZE.x):
		for y in range(SIZE.y):
			
			var height = noise.get_noise_2d(x * 16, y * 16)
			height_map.append(height)
			
			if height > water_height:
				water_tiles.append(Vector2i(x, y))
			
			if height > grass_height:
				grass.set_cell(Vector2(x, y), 4, Vector2(2, 1))
				grass_tiles.append(Vector2i(x, y))
			
			if height > dirt_height:
				dirt_tiles.append(Vector2i(x, y))
	
	water.set_cells_terrain_connect(water_tiles, 0, 2, false)
	grass.set_cells_terrain_connect(grass_tiles, 0, 0, false)
	dirt.set_cells_terrain_connect(dirt_tiles, 0, 3, false)
	
	#grass.update_terrain_cells(0, grass.get_used_cells(), 0, 0)
	

func clear():
	grass.clear()
	dirt.clear()
	water.clear()


func _input(event: InputEvent) -> void:
	if event.is_action_pressed("LeftMouseButton"):
		var clicked_cell = grass.local_to_map(grass.get_local_mouse_position())
		print_debug(height_map[clicked_cell.x + clicked_cell.y])
	
	if event.is_action_pressed("DEBUG"):
		texture_rect.visible = !texture_rect.visible
