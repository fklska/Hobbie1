@tool
@icon("res://world.png")
extends Node2D
class_name MapGenerator

static var SIZE = Vector2i(64, 64)

@export var noise: FastNoiseLite
@export var res_noise: FastNoiseLite

@export_category("Biomes Height")
@export var water_height: float
@export var dirt_height: float
@export var grass_height: float

@onready var dirt: TileMapLayer = $Dirt
@onready var grass: TileMapLayer = $Grass
@onready var water: TileMapLayer = $Water

@export_category("Resourses Height")
@export_range(0, 0.3) var wood_height: float
@export_range(0, 0.3) var rock_height: float
@export_range(0, 0.3) var gold_height: float
@export_range(0, 0.3) var iron_height: float


@onready var RES_TYPES = {
	gold_height: {
		"prefab": preload("res://Resourses/Prefabs/gold.tscn"),
		"sourse_id": 3,
		},
	iron_height: {
		"prefab": preload("res://Resourses/Prefabs/iron.tscn"),
		"sourse_id": 4,
		},
	rock_height: {
		"prefab": preload("res://Resourses/Prefabs/rock.tscn"),
		"sourse_id": 5,
		},
	wood_height: {
		"prefab": preload("res://Resourses/Prefabs/GiantWood.tscn"),
		"sourse_id": 2,
		},
}

@onready var root_node = $Resourses

var dirt_tiles = []
var grass_tiles = []
var water_tiles = []

var height_val =[]
var res_height_val =[]

var gap = 16


func _ready():
	#if Engine.is_editor_hint():
	generate()
	#GlobalNavigation.debug_baking()
	#custom_server()
	#GlobalNavigation.call_deferred("thread_map_bake")

func generate():
	clear()
	
	noise.seed = randi()

	for x in range(-SIZE.x / 2, SIZE.x / 2):
		for y in range(-SIZE.y / 2, SIZE.y / 2):
			
			var height = noise.get_noise_2d(x, y)
			height_val.append(height)
			
			if height > water_height:
				water_tiles.append(Vector2i(x, y))
				
			if height > dirt_height:
				dirt_tiles.append(Vector2i(x, y))
				
			if height > grass_height:
				grass_tiles.append(Vector2i(x, y))
				
				if x % 2 == 0 and y % 2 == 0:
					var res_height = abs(res_noise.get_noise_2d(x, y))
					res_height_val.append(res_height)
					
					for res in RES_TYPES:
						if res_height < res:
							var prefab: Node2D = RES_TYPES[res].get("prefab").instantiate()
							prefab.position = Vector2i(x*gap, y*gap)
							prefab.add_to_group("navigation_polygon_source_geometry_group")
							root_node.add_child(prefab)
							break
					
	# tetsaw
	#print_debug("max: ", height_val.max())
	#print_debug("min: ", height_val.min())
	#print_debug("Resmax: ", res_height_val.max())
	#print_debug("Resmin: ", res_height_val.min())

	water.set_cells_terrain_connect(water_tiles, 0, 2, false)
	grass.set_cells_terrain_connect(grass_tiles, 0, 0, false)
	dirt.set_cells_terrain_connect(dirt_tiles, 0, 3, false)
	#how to rebuild map
	if not Engine.is_editor_hint():
		GlobalNavigation.call_deferred("bake_all_navigation_map")

func clear():
	var objs: Array = root_node.get_children()
	
	for obj in objs:
		obj.queue_free()
		
	grass.clear()
	dirt.clear()
	water.clear()
	dirt_tiles.clear()
	grass_tiles.clear()
	water_tiles.clear()
