@tool

extends Sprite2D

@export var noise: FastNoiseLite
@export var image: Image
@export var color_ramp: Gradient
var pixels: PackedByteArray

func _ready() -> void:
	image = Image.create_empty(256, 256, false, Image.FORMAT_RGBA8)
	pixels = image.get_data()
	texture = ImageTexture.create_from_image(image)
	
func _process(delta: float) -> void:
	noise.offset += delta * Vector3(-20, 0, 0)
	noise.offset.x = fmod(noise.offset.x, 10e7)
	
	for y in range(256):
		for x in range(256):
			var idx = (y * 256 + x) * 4
			var value = noise.get_noise_2d(x, y)
			var col = color_ramp.sample(value)
			pixels[idx]   = int(col.r8)
			pixels[idx+1] = int(col.g8)
			pixels[idx+2] = int(col.b8)
			pixels[idx+3] = int(col.a8)
			
	
	image.set_data(256, 256, false, Image.FORMAT_RGBA8, pixels)
	texture.update(image)
