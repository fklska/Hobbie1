extends Node2D

# Called when the node enters the scene tree for the first time.

func _ready():
	generate_res()
	
@onready var tilemap = $TileMap
var SIZE = Vector2(64, 64)
var LAND_HEIGHT = 0.3

func generate():
	var noise = FastNoiseLite.new()
	noise.seed = 100
	
	var cells = []
	for x in SIZE.x:
		for y in SIZE.y:
			var height = noise.get_noise_2d(x, y)
			if height < LAND_HEIGHT:
				cells.append(Vector2i(x,y))
			else: 
				tilemap.set_cell(0, Vector2i(x,y), 0, Vector2i(11,2), 0)
	tilemap.set_cells_terrain_connect(0, cells, 0, 0)
# Called every frame. 'delta' is the elapsed time since the previous frame.

@export var noise_scale = 0.3
@export var lacunarity = 2.5
@export var RESOURSE_HEIGHT = 0.035
@export var position_gap = 25
@export var offset = Vector2(0, 0)

var wood = preload("res://Resourses/Prefabs/wood.tscn")
func generate_res():
	var noise = FastNoiseLite.new()
	noise.seed = 100
	noise.fractal_lacunarity = lacunarity
	noise.fractal_octaves = 4
	for x in SIZE.x:
		for y in SIZE.y:
			var height = noise.get_noise_2d(x + offset.x, y + offset.y)
			if height < RESOURSE_HEIGHT:
				var prefab = wood.instantiate()
				prefab.position = Vector2(x*position_gap, y*position_gap)
				add_child(prefab)
	
func _process(delta):
	pass
