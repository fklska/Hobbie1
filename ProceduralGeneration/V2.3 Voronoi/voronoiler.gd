@tool
@icon("res://world.png")
extends Node2D

static var SIZE = Vector2i(64, 64)

@export var noise: FastNoiseLite
@export var res_noise: FastNoiseLite

@export_category("Biomes Height")
@export var water_height: float
@export var dirt_height: float
@export var grass_height: float
@export var mountain_height: float

@onready var dirt: TileMapLayer = $Dirt
@onready var grass: TileMapLayer = $Grass
@onready var water: TileMapLayer = $Water
@onready var mountain: TileMapLayer = $Mountain


@export_category("Resourses Height")
@export_range(0, 1) var gold_height: float
@export_range(0, 1) var iron_height: float
@export_range(0, 1) var rock_height: float
@export_range(0, 1) var wood_height: float


@onready var RES_TYPES = {
	gold_height: {
		"prefab": preload("res://Resourses/Prefabs/gold.tscn"),
		"sourse_id": 3,
		"gold_min_h":  gold_height,
		"max_h": 1.0
		},
	iron_height: {
		"prefab": preload("res://Resourses/Prefabs/iron.tscn"),
		"sourse_id": 4,
		"max_h": gold_height
		},
	rock_height: {
		"prefab": preload("res://Resourses/Prefabs/rock.tscn"),
		"sourse_id": 5,
		"max_h": iron_height
		},
	wood_height: {
		"prefab": preload("res://Resourses/Prefabs/wood.tscn"),
		"sourse_id": 2,
		"max_h": 0.27
		},
}

@onready var root_node = $Resourses

var dirt_tiles = []
var grass_tiles = []
var water_tiles = []
var mountain_tiles = []

var height_val =[]
var res_height_val =[]

var gap = 16

@onready var res_texture: TextureRect = $res_texture
@onready var biom_texture: TextureRect = $biom_texture


func _ready():
	#debug_generate()
	generate()
	#biom_texture.texture = ImageTexture.create_from_image(image)
	
func sig(x : float):
	return (1) / (1 + exp(-x))

@export var grad: Gradient
@export var res_grad: Gradient

func generate():
	clear()
	
	noise.seed = randi()
	res_noise.seed = randi()
	var image = Image.create_empty(SIZE.x, SIZE.y, false, Image.FORMAT_RGB8)
	res_texture.texture.noise = res_noise
	for x in range(SIZE.x):
		for y in range(SIZE.y):
			
			var height = (noise.get_noise_2d(x, y) + 1) / 2.0
			var color = grad.sample(height)
			image.set_pixel(x, y, color)
			height_val.append(height)
			
			if height > water_height:
				water_tiles.append(Vector2i(x, y))
				
			if height > dirt_height:
				dirt_tiles.append(Vector2i(x, y))
				
			if height > grass_height:
				grass_tiles.append(Vector2i(x, y))
				if height < mountain_height:
					if x % 2 == 0 and y % 2 == 0:
						var res_height = (res_noise.get_noise_2d(x, y) + 1) / 2
						res_height_val.append(res_height)
						
						for res in RES_TYPES:
							if res < res_height and res_height < RES_TYPES[res].get("max_h"):
								var prefab: Node2D = RES_TYPES[res].get("prefab").instantiate()
								prefab.position = Vector2i(x*gap, y*gap)
								prefab.add_to_group("navigation_polygon_source_geometry_group")
								root_node.add_child(prefab)
								image.set_pixel(x, y, res_grad.sample(res_height))
								break
			
			if height > mountain_height:
				mountain_tiles.append(Vector2i(x, y))
	
	biom_texture.texture = ImageTexture.create_from_image(image)
	#print_debug(height_val)
	# tetsaw
	#print_debug("max: ", height_val.max())
	#print_debug("min: ", height_val.min())
	#print_debug("Resmax: ", res_height_val.max())
	#print_debug("Resmin: ", res_height_val.min())

	water.set_cells_terrain_connect(water_tiles, 0, 2, false)
	grass.set_cells_terrain_connect(grass_tiles, 0, 0, false)
	dirt.set_cells_terrain_connect(dirt_tiles, 0, 3, false)
	mountain.set_cells_terrain_connect(mountain_tiles, 0, 4, false)
	#how to rebuild map
	if not Engine.is_editor_hint():
		GlobalNavigation.call_deferred("bake_all_navigation_map")

@export var Debug_size: Vector2i

func debug_generate():
	clear()
	noise.seed = randi()
	res_noise.seed = randi()
	var image = Image.create_empty(Debug_size.x, Debug_size.y, false, Image.FORMAT_RGB8)
	res_texture.texture.noise = res_noise
	for x in range(Debug_size.x):
		for y in range(Debug_size.y):
			var height = (noise.get_noise_2d(x, y) + 1) / 2.0
			var color = grad.sample(height)
			image.set_pixel(x, y, color)
			height_val.append(height)

	biom_texture.texture = ImageTexture.create_from_image(image)
	#print_debug(height_val)


func clear():
	var objs: Array = root_node.get_children()
	
	for obj in objs:
		obj.queue_free()
		
	grass.clear()
	dirt.clear()
	water.clear()
	mountain.clear()
	dirt_tiles.clear()
	grass_tiles.clear()
	water_tiles.clear()
	mountain_tiles.clear()
