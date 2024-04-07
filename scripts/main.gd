extends Node2D
class_name MapGenerator

static var SIZE = Vector2i(32, 32)

@export var noise: FastNoiseLite
@export var res_noise: FastNoiseLite

@export_category("Biomes Height")
@export_range(-0.8, 0) var water_height: float
@export_range(-0.4, 0.8) var sand_height: float

@export_category("Resourses Height")
@export_range(0, 0.3) var wood_height: float
@export_range(0, 0.3) var rock_height: float
@export_range(0, 0.3) var gold_height: float
@export_range(0, 0.3) var iron_height: float

@onready var tilemap: TileMap = $Tilemap
@onready var player = $Player_MainCharacter
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

@onready var root_node = $Navigation

var sand_tiles = []
var grass_tiles = []
var water_tiles = []

var height_val =[]
var res_height_val =[]

var gap = 64

func _ready():
	generate()
	custom_server()

func generate():
	clear()
	
	noise.seed = randi()
	noise.offset = Vector3(player.global_position.x, player.global_position.y, 0)

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
						root_node.add_child(prefab)
						break
					
				grass_tiles.append(Vector2i(x, y))


	#print_debug("max: ", height_val.max())
	#print_debug("min: ", height_val.min())
	#print_debug("Resmax: ", res_height_val.max())
	#print_debug("Resmin: ", res_height_val.min())

	tilemap.set_cells_terrain_connect(0, water_tiles, 0, 0)
	tilemap.set_cells_terrain_connect(0, sand_tiles, 0, 1)
	tilemap.set_cells_terrain_connect(0, grass_tiles, 0, 2)

func clear():
	var objs: Array = root_node.get_children()
	
	for obj in objs:
		obj.queue_free()
		
	tilemap.clear()
	water_tiles.clear()
	sand_tiles.clear()
	grass_tiles.clear()

func setup_polygon(current_position: Vector2):
	#nav_mesh.navigation_polygon.clear_polygons()
	#nav_mesh.navigation_polygon.clear_outlines()
	var coor = current_position
	#var bounding_outline = PackedVector2Array([Vector2(-SIZE.x * 32, -SIZE.y * 32), Vector2(SIZE.x * 32 / 8, -SIZE.y * 32), Vector2(SIZE.x * 32 / 8, SIZE.y * 32 / 8), Vector2(-SIZE.x * 32, SIZE.y * 32 / 8)])
	var _bounding_outline = PackedVector2Array([Vector2(coor.x - 256, coor.y - 256), Vector2(coor.x + 256, coor.y - 256), Vector2(coor.x + 256, coor.y + 256), Vector2(coor.x - 256, coor.y + 256)])
	#nav_mesh.navigation_polygon.add_outline(bounding_outline)
	print_debug("Start Baking")
	#nav_mesh.bake_navigation_polygon(true)

var navigation_mesh: NavigationPolygon
var source_geometry : NavigationMeshSourceGeometryData2D
var callback_parsing : Callable
var callback_baking : Callable
var region_rid: RID

func custom_server():
	navigation_mesh = NavigationPolygon.new()
	navigation_mesh.agent_radius = 15.0
	source_geometry = NavigationMeshSourceGeometryData2D.new()

	callback_parsing = on_parsing_done
	callback_baking = on_baking_done
	
	region_rid = NavigationServer2D.region_create()
	NavigationServer2D.region_set_enabled(region_rid, true)
	NavigationServer2D.region_set_map(region_rid, get_world_2d().get_navigation_map())
	NavigationServer2D.set_debug_enabled(true)

	parse_source_geometry.call_deferred()

func parse_source_geometry() -> void:
	source_geometry.clear()

	NavigationServer2D.parse_source_geometry_data(
		navigation_mesh,
		source_geometry,
		root_node,
		callback_parsing
	)

func on_parsing_done() -> void:
	source_geometry.add_traversable_outline(
		PackedVector2Array(
			[
				Vector2(-SIZE.x * 32, -SIZE.y * 32),
				Vector2(SIZE.x * 32, -SIZE.y * 32),
				Vector2(SIZE.x * 32, SIZE.y * 32),
				Vector2(-SIZE.x * 32, SIZE.y * 32)
			]
		)
	)
	NavigationServer2D.bake_from_source_geometry_data_async(
		navigation_mesh,
		source_geometry,
		callback_baking
	)

func on_baking_done() -> void:
	# Update the region with the updated navigation mesh.
	NavigationServer2D.region_set_navigation_polygon(region_rid, navigation_mesh)
