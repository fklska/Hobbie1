extends StaticBodySelectedObject

@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var animation: AnimationPlayer = $AnimationPlayer

func _ready():
	shader = anim.material
	animation.play("idle")

func get_texture():
	return anim.sprite_frames.get_frame_texture("idle", 0)

func send_obj_data() -> Dictionary:
	return {
		"Description": "Кузница"
	}
