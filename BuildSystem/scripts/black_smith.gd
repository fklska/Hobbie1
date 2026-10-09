extends Building

@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var animation: AnimationPlayer = $AnimationPlayer

func _ready():
	super()
	shader = anim.material
	animation.play("idle")

func get_texture():
	return anim.sprite_frames.get_frame_texture("idle", 0)

func preview(_value: int) -> Dictionary:
	var sprite := get_node("AnimatedSprite2D") as AnimatedSprite2D
	var tex := sprite.sprite_frames.get_frame_texture("idle", 0)
	var size := tex.get_size() * sprite.scale
	return {"texture": tex, "rect": Rect2(sprite.position - size / 2, size)}
