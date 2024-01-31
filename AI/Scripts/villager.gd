extends CharacterBody2D
class_name VillagerClass

@export var data: AIBackData
@export var player: Player
@onready var nav: NavigationAgent2D = $NavigationAgent2D

@onready var anim: AnimationPlayer = $AnimationPlayer
@onready var anim_sprite: AnimatedSprite2D = $AnimatedSprite2D


const SPEED = 50

enum {
	RUN,
	GRINDING,
}

var state = RUN
func _ready():
	nav.target_position = player.global_position

func _physics_process(delta):
	match state:
		RUN:
			run()
		GRINDING:
			grind()


func run():
	var dir = to_local(nav.get_next_path_position()).normalized()
	velocity = dir * SPEED * data.AGILITY
	
	move_and_slide()

func grind():
	pass

#var targets: Array[ActiveResourses] = []
#func _on_area_2d_body_entered(body: ActiveResourses):
#	print_debug(body)
#	if body.collision_layer == 4:
#		targets.append(body)
#	nav.target_position = targets[0].global_position


#func _on_area_2d_body_exited(body):
#	if body.collision_layer == 4:
#		targets.erase(body)

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
		
		if SelectorClass.selected_object == self:
			nav.target_position = get_global_mouse_position()
			
func show_selected_info():
	return {
		"texture": anim_sprite.sprite_frames.get_frame_texture("idle", 0),
		"text": ("")
	}




