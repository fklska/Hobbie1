extends AnimatedSprite2D
class_name DirectionalSprite

const SUFFIXES := ["e", "se", "s", "sw", "w", "nw", "n", "ne"]

@export var facing := 2

var action := ""


func face(dir: Vector2) -> void:
	if dir.length_squared() > 0.0001:
		facing = posmod(roundi(dir.angle() / (PI / 4.0)), 8)


func anim_name(act: String) -> StringName:
	var directional := "%s_%s" % [act, SUFFIXES[facing]]
	return StringName(directional if sprite_frames.has_animation(directional) else act)


func play_dir(act: String, dir := Vector2.ZERO, restart := false) -> void:
	face(dir)
	var next := anim_name(act)
	if next == animation and not restart and is_playing():
		return
	var keep := act == action and not restart
	var f := frame
	var p := frame_progress
	action = act
	play(next)
	if keep and f < sprite_frames.get_frame_count(next):
		set_frame_and_progress(f, p)


func action_length(act: String) -> float:
	var anim := anim_name(act)
	if not sprite_frames.has_animation(anim):
		return 0.0
	var speed := sprite_frames.get_animation_speed(anim)
	return sprite_frames.get_frame_count(anim) / speed if speed > 0.0 else 0.0
