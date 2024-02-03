extends CharacterBody2D
class_name BaseVillager

@export var data: AIBackData

@onready var nav: NavigationAgent2D = $NavigationAgent2D

@onready var anim: AnimationPlayer = $AnimationPlayer
@onready var anim_sprite: AnimatedSprite2D = $AnimatedSprite2D

const SPEED = 50

enum {
	RUN,
	ATTACK
}

var state = RUN

var enemy_target: Node2D

func _physics_process(delta):
	match state:
		RUN:
			run()
		ATTACK:
			attack()

func run():
	var dir = to_local(nav.get_next_path_position()).normalized()
	if dir:
		velocity = dir * SPEED * data.AGILITY
		anim.play("walk")
		if dir > Vector2(0, 0):
			anim_sprite.scale.x = -1
		
		if dir < Vector2(0, 0):
			anim_sprite.scale.x = 1
	
	if enemy_target:
		nav.target_position = enemy_target.global_position
		var distance = global_position.distance_to(enemy_target.global_position)
		if distance <= 50:
			state = ATTACK
	else:
		if nav.is_navigation_finished():
			velocity = Vector2(0, 0)
			anim.play("idle")

	move_and_slide()

func attack():
	anim.play("attack")

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton"):
		if SelectorClass.selected_object == self:
			nav.target_position = get_global_mouse_position()

func show_selected_info():
	return {
		"texture": anim_sprite.sprite_frames.get_frame_texture("idle", 0),
		"text": ("")
	}

func _to_string():
	return "Base Villager"

func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("RightMouseButton"):
		SelectorClass.selected_object = self

func _on_enemy_trigger_body_entered(body):
	enemy_target = body
	state = RUN

func _on_enemy_trigger_body_exited(body):
	enemy_target = null
	await anim.animation_finished
	state = RUN

func _on_weapon_area_body_entered(body):
	print_debug("Get Hit")
