extends CanvasLayer


func _process(delta):
	show_player(player.get_node("AnimatedSprite2D"))

func _input(event):
	if event.is_action_pressed("inventory"):
		visible = !visible

@onready var player: Player = $"../Player"
@onready var avatar = $TextureRect/TextureRect2/Avatar
func show_player(sprite: AnimatedSprite2D):
	var animation = sprite.animation
	var frame = sprite.frame
	var current_frame = sprite.sprite_frames.get_frame_texture(animation, frame)
	avatar.texture = current_frame
