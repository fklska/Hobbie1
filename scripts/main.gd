extends Node2D
class_name MapGenerator

static var SIZE = Vector2i(64, 64)

@export var noise: FastNoiseLite
@export var res_noise: FastNoiseLite
@export var nav_mesh: NavigationRegion2D

@export_category("Biomes Height")
@export_range(-0.8, 0) var water_height: float
@export_range(-0.4, 0.8) var sand_height: float

@export_category("Resourses Height")
@export_range(0, 0.3) var wood_height: float
@export_range(0, 0.3) var rock_height: float
@export_range(0, 0.3) var gold_height: float
@export_range(0, 0.3) var iron_height: float

@onready var tilemap: TileMap = $NavigationRegion2D/Tilemap
@onready var player = $Player
@onready var RES_TYPES = {
		gold_height: {
			"prefab": preload("res://Resourses/Prefabs/gold.tscn"),
			"sourse_id": 3
			},
		iron_height: {
			"prefab": preload("res://Resourses/Prefabs/iron.tscn"),
			"sourse_id": 4
			},
		rock_height: {
			"prefab": preload("res://Resourses/Prefabs/rock.tscn"),
			"sourse_id": 5
			},
		wood_height: {
			"prefab": preload("res://Resourses/Prefabs/wood.tscn"),
			"sourse_id": 2
			},
	}

var sand_tiles = []
var grass_tiles = []
var water_tiles = []

var height_val =[]
var res_height_val =[]

var gap = 64

func _ready():
	noise.seed = randi()
	noise.offset = Vector3(player.global_position.x, player.global_position.y, 0)
	print_debug(noise.seed)
	generate()
	
	setup_polygon()

func generate():
	for x in range(-SIZE.x / 2, SIZE.x / 2):
		for y in range(-SIZE.y / 2, SIZE.y / 2):
			
			var height = noise.get_noise_2d(x, y)
			height_val.append(height)
			
			if height < water_height:
				water_tiles.append(Vector2i(x, y))
				
			elif height < sand_height:
				sand_tiles.append(Vector2i(x, y))
				
			else:
				var res_height = abs(res_noise.get_noise_2d(x, y))
				res_height_val.append(res_height)
				
				for res in RES_TYPES:
					if res_height < res:
						var prefab: Node2D = RES_TYPES[res].get("prefab").instantiate()
						prefab.position = Vector2i(x*gap, y*gap)
						prefab.add_to_group("navigation_polygon_source_group")
						nav_mesh.add_child(prefab)
						break
					
				grass_tiles.append(Vector2i(x, y))


	#print_debug("max: ", height_val.max())
	#print_debug("min: ", height_val.min())
	print_debug("Resmax: ", res_height_val.max())
	print_debug("Resmin: ", res_height_val.min())

	tilemap.set_cells_terrain_connect(0, water_tiles, 0, 0)
	tilemap.set_cells_terrain_connect(0, sand_tiles, 0, 1)
	tilemap.set_cells_terrain_connect(0, grass_tiles, 0, 2)


func setup_polygon():
	var bounding_outline = PackedVector2Array([Vector2(-SIZE.x * 32, -SIZE.y * 32), Vector2(SIZE.x * 32, -SIZE.y * 32), Vector2(SIZE.x * 32, SIZE.y * 32), Vector2(-SIZE.x * 32, SIZE.y * 32)])
	nav_mesh.navigation_polygon.add_outline(bounding_outline)
	print_debug("Start Baking")
	nav_mesh.bake_navigation_polygon(true)


func _on_navigation_region_2d_bake_finished():
	print_debug("bake FInished")
