extends StaticBodySelectedObject
class_name Building

signal destroyed

@export var data: StorageDataClass
@export var max_hp: int = 300
@export var footprint := Vector2i(1, 1)

var hp: int
var hp_bar: ProgressBar

func _ready():
	super()
	hp = max_hp
	add_to_group("village")
	hp_bar = ProgressBar.new()
	hp_bar.show_percentage = false
	hp_bar.max_value = max_hp
	hp_bar.value = hp
	hp_bar.size = Vector2(footprint.x * 64, 6)
	hp_bar.position = Vector2(0, -10)
	hp_bar.visible = false
	add_child(hp_bar)

func get_center() -> Vector2:
	return global_position + Vector2(footprint * 32)

func take_damage(amount: int):
	if hp <= 0:
		return
	hp = max(0, hp - amount)
	hp_bar.value = hp
	hp_bar.visible = true
	modulate = Color(1, 0.6, 0.6)
	create_tween().tween_property(self, "modulate", Color.WHITE, 0.2)
	if hp == 0:
		destroyed.emit()
		queue_free()

func get_texture():
	return texture.texture
