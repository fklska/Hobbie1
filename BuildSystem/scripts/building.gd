extends StaticBodySelectedObject
class_name Building

signal destroyed

@export var data: StorageDataClass
@export var building_id := ""
@export var max_hp: int = 300
@export var footprint := Vector2i(1, 1)
@export var level_textures: Array[Texture2D] = []
@export var level_parts: Array[Resource] = []
@export var fit_texture := true
@export var night_lamp := true

const GROUND_LIFT := 64.0

var hp: int
var level := 1
var hp_bar: ProgressBar
var _parts: Array[Sprite2D] = []

func _ready():
	super()
	y_sort_enabled = true
	shader.set_shader_parameter("reveal_hero", true)
	hp = max_hp
	add_to_group("village")
	hp_bar = ProgressBar.new()
	hp_bar.z_index = 1
	hp_bar.show_percentage = false
	hp_bar.max_value = max_hp
	hp_bar.value = hp
	hp_bar.size = Vector2(footprint.x * 64, 6)
	hp_bar.position = Vector2(0, -10)
	hp_bar.visible = false
	add_child(hp_bar)
	_update_visual()

func set_level(value: int, new_max_hp: int):
	level = value
	max_hp = new_max_hp
	hp = max_hp
	hp_bar.max_value = max_hp
	hp_bar.value = hp
	hp_bar.visible = false
	_update_visual()

func restore_hp(value: int):
	hp = clampi(value, 1, max_hp)
	hp_bar.value = hp
	hp_bar.visible = hp < max_hp

func level_texture(value: int) -> Texture2D:
	if level_textures.is_empty():
		return null
	return level_textures[clampi(value, 1, level_textures.size()) - 1]

func level_layers(value: int) -> BuildingParts:
	if level_parts.is_empty():
		return null
	return level_parts[clampi(value, 1, level_parts.size()) - 1] as BuildingParts

func fit_rect(tex: Texture2D) -> Rect2:
	var size := tex.get_size() * (footprint.x * 64.0 / tex.get_width())
	return Rect2(Vector2(footprint.x * 32.0 - size.x / 2, footprint.y * 64.0 - size.y), size)

func preview(value: int) -> Dictionary:
	var sprite := get_node("Texture") as Sprite2D
	var tex := level_texture(value)
	if tex == null:
		tex = sprite.texture
	if tex == null:
		return {}
	if fit_texture:
		return {"texture": tex, "rect": fit_rect(tex)}
	var rect := Rect2(sprite.offset - (tex.get_size() / 2 if sprite.centered else Vector2.ZERO), tex.get_size())
	return {"texture": tex, "rect": Rect2(sprite.position + rect.position * sprite.scale, rect.size * sprite.scale)}

func _update_visual():
	var tex := level_texture(level)
	if tex:
		texture.texture = tex
	var layers := level_layers(level)
	if layers and layers.ground:
		texture.texture = layers.ground
	if fit_texture and texture.texture:
		var rect := fit_rect(texture.texture)
		texture.centered = false
		texture.scale = rect.size / texture.texture.get_size()
		var sort_y := rect.position.y - GROUND_LIFT if layers else rect.end.y
		texture.position = Vector2(rect.position.x, sort_y)
		texture.offset = Vector2(0, (rect.position.y - sort_y) / texture.scale.y)
	texture.z_index = -1 if layers else 0
	_build_parts(layers)

func _build_parts(layers: BuildingParts):
	for part in _parts:
		part.queue_free()
	_parts.clear()
	if layers == null or texture.texture == null:
		return
	var top_left := texture.position + (texture.offset - (texture.texture.get_size() / 2 if texture.centered else Vector2.ZERO)) * texture.scale
	for i in layers.parts.size():
		var part := Sprite2D.new()
		part.texture = layers.parts[i]
		part.centered = false
		part.material = texture.material
		part.scale = texture.scale
		part.position = top_left + Vector2(0, layers.sort_rows[i] * texture.scale.y)
		part.offset = layers.positions[i] - Vector2(0, layers.sort_rows[i])
		add_child(part)
		_parts.append(part)

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
		Sound.PlayAt("collapse", get_center())
		destroyed.emit()
		queue_free()

func get_texture():
	var tex := level_texture(level)
	return tex if tex else texture.texture

func heal(amount: int):
	hp = mini(max_hp, hp + amount)
	hp_bar.value = hp
	hp_bar.visible = hp < max_hp

func send_obj_data() -> Dictionary:
	var stats: Dictionary = Game.GetLevelStats(building_id) if building_id else {}
	return {
		"Description": "%s, ур. %d" % [stats.title, level] if stats else Name,
		"HP": "%d/%d" % [hp, max_hp]
	}
