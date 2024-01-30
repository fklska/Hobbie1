extends Node2D

# Called when the node enters the scene tree for the first time.
@onready var player = $Player
@export var noise: FastNoiseLite
@export var res_noise: FastNoiseLite

var RES_TYPES = {}

func _ready():
	noise.seed = randi()
	noise.offset = Vector3(player.global_position.x, player.global_position.y, 0)
	print_debug(noise.seed)
	RES_TYPES = {
		gold_height: {
			"prefab": gold,
			"sourse_id": 3
			},
		iron_height: {
			"prefab": iron,
			"sourse_id": 4
			},
		rock_height: {
			"prefab": rock,
			"sourse_id": 5
			},
		wood_height: {
			"prefab": wood,
			"sourse_id": 2
			},
	}
	generate()

@export var SIZE = Vector2i(64, 64)

@onready var tilemap: TileMap = $Tilemap

var sand_tiles = []
var grass_tiles = []
var water_tiles = []

var height_val =[]
var res_height_val =[]

@export_range(-0.8, 0) var water_height: float
@export_range(-0.4, 0.8) var sand_height: float

@export_range(0, 0.3) var wood_height: float
@export_range(0, 0.3) var rock_height: float
@export_range(0, 0.3) var gold_height: float
@export_range(0, 0.3) var iron_height: float

@onready var wood = preload("res://Resourses/Prefabs/wood.tscn")
@onready var gold = preload("res://Resourses/Prefabs/gold.tscn")
@onready var iron = preload("res://Resourses/Prefabs/iron.tscn")
@onready var rock = preload("res://Resourses/Prefabs/rock.tscn")

var gap = 64
func generate():
	for x in range(-SIZE.x / 2, SIZE.x / 2):
		for y in range(-SIZE.y / 2, SIZE.y / 2):
			
			var height = noise.get_noise_2d(x, y)
			height_val.append(height)
			#tilemap.set_cell(0, Vector2(tile_pos.x - SIZE.x / 2 + x, tile_pos.y - SIZE.y / 2 + y), 0, get_tile_atlas_coord(height))		
			if height < water_height:
				water_tiles.append(Vector2i(x, y))
			elif height < sand_height:
				sand_tiles.append(Vector2i(x, y))
			else:
				var res_height = abs(res_noise.get_noise_2d(x, y))
				res_height_val.append(res_height)
				
				for res in RES_TYPES:
					if res_height < res:
						var prefab = RES_TYPES[res].get("prefab").instantiate()
						prefab.position = Vector2i(x*gap, y*gap)
						add_child(prefab)
						#tilemap.set_cell(1, Vector2i(x, y), RES_TYPES[res].get("sourse_id"), Vector2i(0, 0))
						break

				grass_tiles.append(Vector2i(x, y))
			


	#print_debug("max: ", height_val.max())
	#print_debug("min: ", height_val.min())
	print_debug("Resmax: ", res_height_val.max())
	print_debug("Resmin: ", res_height_val.min())

	tilemap.set_cells_terrain_connect(0, water_tiles, 0, 0)
	tilemap.set_cells_terrain_connect(0, sand_tiles, 0, 1)
	tilemap.set_cells_terrain_connect(0, grass_tiles, 0, 2)	
