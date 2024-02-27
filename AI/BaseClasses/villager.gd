extends CharacterBody2D
class_name BaseVillager

@export var data: AIBackData

@onready var nav: NavigationAgent2D = $NavigationAgent2D
#@onready var main: MapGenerator = $".."


@onready var anim: AnimationPlayer = $AnimationPlayer
@onready var anim_sprite: AnimatedSprite2D = $AnimatedSprite2D

const SPEED = 50

enum {
	RUN,
	ATTACK,
	SEARCH
}

var state = RUN

var enemy_target

var firs_pos: Vector2

#@onready var nav_mesh: NavigationRegion2D

func _ready():
	#nav_mesh = $"../NavigationRegion2D"
	#var new_polygon: NavigationPolygon = NavigationPolygon.new()
	#new_polygon.source_geometry_mode = NavigationPolygon.SOURCE_GEOMETRY_GROUPS_WITH_CHILDREN
	#new_polygon.agent_radius = 10
	#nav_mesh.navigation_polygon = new_polygon
	#setup_polygon()
	firs_pos = global_position

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
			
	#if abs(global_position.x - firs_pos.x) + abs(global_position.y - firs_pos.y) >= 170:
	#	setup_polygon()
	#	firs_pos = global_position

	move_and_slide()

func attack():
	anim.play("attack")

var index: int = 0
func setup_polygon():
	var coor = global_position
	var bounding_outline = PackedVector2Array([Vector2(coor.x - 256, coor.y - 256), Vector2(coor.x + 256, coor.y - 256), Vector2(coor.x + 256, coor.y + 256), Vector2(coor.x - 256, coor.y + 256)])
	#nav_mesh.navigation_polygon.add_outline_at_index(bounding_outline, index)
	print_debug("Start Baking")
	#nav_mesh.bake_navigation_polygon(true)
	
	#if index != 0:
		#nav_mesh.navigation_polygon.remove_outline(index-1)
	#print_debug(nav_mesh.navigation_polygon.get_outline(index))
	index += 1
	

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton"):
		if RectSelectorClass.selected_object.has(self):
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
		#SelectorClass.selected_object = self
		pass

func _on_enemy_trigger_body_entered(body):
	enemy_target = body
	state = RUN

func _on_enemy_trigger_body_exited(body):
	enemy_target = null
	await anim.animation_finished
	state = RUN

func _on_weapon_body_entered(body):
	print_debug("Get Hit")
