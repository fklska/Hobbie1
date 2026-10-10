extends AnimatedSprite2D

const GLOW := {
	&"flame": Color(1.35, 1.25, 1.15),
	&"fire_burst": Color(1.35, 1.25, 1.15),
	&"meteor": Color(1.4, 1.3, 1.2),
	&"fireball": Color(1.4, 1.3, 1.2),
	&"explosion": Color(1.15, 1.08, 1.0),
	&"summon": Color(1.15, 1.35, 1.15),
}
const OFFSETS := {
	&"flame": Vector2(0, -13),
	&"fire_burst": Vector2(0, -12),
	&"meteor": Vector2(0, -67),
	&"fireball": Vector2(0, -18),
	&"explosion": Vector2(0, -41),
	&"shockwave": Vector2(0, -6),
	&"whirl": Vector2(0, -9),
	&"summon": Vector2(0, -20),
}
const UNSHADED := preload("res://Game/Skills/unshaded.tres")


func play_fx(anim_name: StringName) -> void:
	offset = OFFSETS.get(anim_name, Vector2.ZERO)
	if GLOW.has(anim_name):
		material = UNSHADED
		self_modulate = GLOW[anim_name]
	play(anim_name)
	if not sprite_frames.get_animation_loop(anim_name):
		animation_finished.connect(queue_free)
