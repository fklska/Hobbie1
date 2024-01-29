extends CharacterBody2D
class_name VillagerClass

@export var data: AIBackData
@export var player: Player
@onready var nav: NavigationAgent2D = $NavigationAgent2D
@onready var anim: AnimationPlayer = $AnimationPlayer



const SPEED = 50

func _ready():
	nav.target_position = player.global_position

func _physics_process(delta):
	var current_pos = global_position
	velocity = current_pos.direction_to(nav.get_next_path_position()) * SPEED 
	#animPlayer.play("run")
		
	#if direction.x > 0:
		#anim.scale.x = -1
	#	pass
		
	#if direction.x < 0:
		#anim.scale.x = 1
	#	pass
	
	move_and_slide()


#var targets: Array[ActiveResourses] = []
#func _on_area_2d_body_entered(body: ActiveResourses):
#	print_debug(body)
#	if body.collision_layer == 4:
#		targets.append(body)
#	nav.target_position = targets[0].global_position


#func _on_area_2d_body_exited(body):
#	if body.collision_layer == 4:
#		targets.erase(body)
