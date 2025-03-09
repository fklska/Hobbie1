extends KinematicBodyEntity
class_name BaseVillager

@export var data: AIBackData
@export var tick: float
@onready var nav: NavigationAgent2D = $NavigationAgent2D
@onready var anim_player: AnimationPlayer = $AnimationPlayer
@onready var timer: Timer = $Timer

const SPEED = 50

enum {
	RUN,
	ATTACK,
	SEARCH
}

var state = RUN

var enemy_target

var current_cell: Vector2i = Vector2i(0, 0)

var Path: PackedVector2Array = PackedVector2Array()
#@onready var nav_mesh: NavigationRegion2D

func _ready() -> void:
	shader = anim.material
	if shader == null:
		print_debug("SHADER INSTALL")
	
	timer.wait_time = randf_range(5, 15)
	data.AGILITY = randi_range(2, 5)
	
	#await Navigation.bake_navigation_on_agent(self)

func _physics_process(_delta):
	match state:
		RUN:
			run()
		ATTACK:
			attack()

func run():
	var dir = to_local(nav.get_next_path_position()).normalized()
	if dir:
		velocity = dir * SPEED * data.AGILITY
		anim_player.play("walk")
		if dir > Vector2(0, 0):
			anim.scale.x = -1
		
		if dir < Vector2(0, 0):
			anim.scale.x = 1
	
	if enemy_target:
		nav.target_position = enemy_target.global_position
		var distance = global_position.distance_to(enemy_target.global_position)
		if distance <= 50:
			state = ATTACK
	else:
		if nav.is_navigation_finished():
			velocity = Vector2(0, 0)
			anim_player.play("idle")
			
	#if current_cell != Navigation.StaticPixel2cell(global_position):
		#GlobalNavigation.call_deferred("thread_bake", self)


	move_and_slide()

func attack():
	anim_player.play("attack")

var index: int = 0
func setup_polygon():
	var coor = global_position
	var _bounding_outline = PackedVector2Array([Vector2(coor.x - 256, coor.y - 256), Vector2(coor.x + 256, coor.y - 256), Vector2(coor.x + 256, coor.y + 256), Vector2(coor.x - 256, coor.y + 256)])
	#nav_mesh.navigation_polygon.add_outline_at_index(bounding_outline, index)
	print_debug("Start Baking")
	#nav_mesh.bake_navigation_polygon(true)
	
	#if index != 0:
		#nav_mesh.navigation_polygon.remove_outline(index-1)
	#print_debug(nav_mesh.navigation_polygon.get_outline(index))
	index += 1
	

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton"):
		nav.target_position = get_global_mouse_position()

func show_selected_info():
	return {
		"texture": anim.sprite_frames.get_frame_texture("idle", 0),
		"text": ("")
	}

func _to_string():
	return "Base Villager"

func _on_enemy_trigger_body_entered(body):
	enemy_target = body
	state = RUN

func _on_enemy_trigger_body_exited(_body):
	enemy_target = null
	await anim_player.animation_finished
	state = RUN

func _on_weapon_body_entered(_body):
	print_debug("Get Hit")

func get_texture():
	pass

func send_obj_data() -> Dictionary:
	return {
		"Basic": "Basic KinematicBody2dObject",
		"2nd": "Second label"
	}

func get_next_path_pos():
	if Path.is_empty():
		return global_position

	var value = Path[0]
	Path.remove_at(0)
	return value

func _on_timer_timeout() -> void:
	var dest = get_global_mouse_position() + Vector2(randi_range(-100, 100), randi_range(-100, 100))
	#Path = NavigationServer2D.map_get_path(get_world_2d().navigation_map, global_position, dest, true)
	nav.target_position = dest
