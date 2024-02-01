extends CharacterBody2D
class_name VillagerClass

@export var data: AIBackData
@onready var nav: NavigationAgent2D = $NavigationAgent2D

@onready var anim: AnimationPlayer = $AnimationPlayer
@onready var anim_sprite: AnimatedSprite2D = $AnimatedSprite2D


const SPEED = 50

enum {
	RUN,
	GRINDING,
}

var state = RUN

func _physics_process(delta):
	match state:
		RUN:
			run()
		GRINDING:
			grind()


func run():
	var dir = to_local(nav.get_next_path_position()).normalized()
	velocity = dir * SPEED * data.AGILITY
	
	if Input.is_action_just_pressed("LeftMouseButton"):
		if SelectorClass.selected_object == self:
			nav.target_position = get_global_mouse_position()
			state = RUN

	if nav.is_navigation_finished():
		velocity = Vector2(0, 0)

	move_and_slide()

@onready var trigger_area: Area2D = $Area2D
func grind():
	var targets: Array[Node2D] = trigger_area.get_overlapping_bodies()
	print_debug(targets)


var mouse_enter: bool = false
func _on_mouse_entered():
	modulate = Color8(155, 155, 155, 255)
	mouse_enter = true


func _on_mouse_exited():
	modulate = Color8(255, 255, 255, 255)
	mouse_enter = false


func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("LeftMouseButton"):
		if mouse_enter:
			SelectorClass.selected_object = self
			
func show_selected_info():
	return {
		"texture": anim_sprite.sprite_frames.get_frame_texture("idle", 0),
		"text": ("")
	}

func _to_string():
	return "Villager"
