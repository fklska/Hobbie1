@tool
extends Node2D

@export var HeightNoise: FastNoiseLite
@export var TempNoise: FastNoiseLite

@export var HeightGradRender: Gradient

@export var TempGradRenderWater: Gradient
@export var TempGradRenderMountain: Gradient

@export var size: int
@onready var texture_rect: TextureRect = $TextureRect

@export var IslandMask: GradientTexture2D
@export var PolusMask: GradientTexture2D

@export var Test: GradientTexture2D
func _ready() -> void:
	#PolusRender()
	#IslandRender()
	defaultRender()

func defaultRender():
	var image = Image.create_empty(size, size, false, Image.FORMAT_RGB8)
	HeightNoise.seed = randi()

	for x in range(size):
		for y in range(size):
			var height = (HeightNoise.get_noise_2d(x, y) + 1) / 2
			var temp = (TempNoise.get_noise_2d(x, y) + 1) / 2
			var color = null
			if height > 0.5:
				color = TempGradRenderMountain.sample(temp)
			else:
				color = TempGradRenderWater.sample(temp)
			#var color = HeightGradRender.sample(height) #* maskImage.get_pixel(x, y)
			image.set_pixel(x, y, color)
	
	texture_rect.texture = ImageTexture.create_from_image(image)

func IslandRender():
	IslandMask.width = size
	IslandMask.height = size
	var maskImage: Image = IslandMask.get_image()
	var image = Image.create_empty(size, size, false, Image.FORMAT_RGB8)
	
	HeightNoise.seed = randi()

	for x in range(size):
		for y in range(size):
			var height = (HeightNoise.get_noise_2d(x, y) + 1) / 2
			height = height * maskImage.get_pixel(x, y).r
			var color = HeightGradRender.sample(height) #* maskImage.get_pixel(x, y)
			image.set_pixel(x, y, color)
	
	texture_rect.texture = ImageTexture.create_from_image(image)

func PolusRender():
	PolusMask.width = size
	PolusMask.height = size
	var maskImage: Image = PolusMask.get_image()
	var image = Image.create_empty(size, size, false, Image.FORMAT_RGB8)
	
	HeightNoise.seed = randi()

	for x in range(size):
		for y in range(size):
			var height = (HeightNoise.get_noise_2d(x, y) + 1) / 2
			height = (height + maskImage.get_pixel(x, y).r) / 2
			var color = HeightGradRender.sample(height) #* maskImage.get_pixel(x, y)
			image.set_pixel(x, y, color)
	
	texture_rect.texture = ImageTexture.create_from_image(image)
