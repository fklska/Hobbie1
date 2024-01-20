extends CanvasLayer

@onready var player = $"../Player"
@onready var portret: TextureRect = $MarginContainer/Panel/SelectorInfo/Portret
func _ready():
	pass

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	var sprite: AnimatedSprite2D = player.get_child(1)
	var frames: SpriteFrames = sprite.sprite_frames
	portret.texture = frames.get_frame_texture(sprite.animation, sprite.frame)
