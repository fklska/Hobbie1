extends CharacterBody2D
class_name VillagerClass

@export var data: AIBackData
@export var circle_detector: Panel

@onready var nav: NavigationAgent2D = $NavigationAgent2D
@onready var nav_mesh: NavigationRegion2D = $"../NavigationRegion2D"

@onready var storage: Building = $"../NavigationRegion2D/TownHall"

@onready var anim: AnimationPlayer = $AnimationPlayer
@onready var anim_sprite: AnimatedSprite2D = $AnimatedSprite2D

@onready var trigger_area: Area2D = $Area2D

const SPEED = 50

enum {
	IDLE,
	GRINDING,
	SET_TARGET,
	DETECTING,
	GO_TO_STORAGE
}

var state = IDLE

var mouse_enter: bool = false

var objects: Array[Node2D]

func _physics_process(delta):
	match state:
		IDLE:
			idle()
		SET_TARGET:
			run()
		DETECTING:
			detecting()
		GRINDING:
			grind()
		GO_TO_STORAGE:
			run()

func idle():
	anim.play("idle")

func run():
	var dir = to_local(nav.get_next_path_position()).normalized()
	if dir:
		velocity = dir * SPEED * data.AGILITY
		anim.play("walk")
		
		if dir > Vector2(0, 0):
			anim_sprite.scale.x = 1
		
		if dir < Vector2(0, 0):
			anim_sprite.scale.x = -1

	if nav.is_navigation_finished():
		if state == GO_TO_STORAGE:
			for i: InventoryItem in data.Inventory:
				storage.data.put_in_storage(i)
			state = IDLE
		else:
			state = DETECTING

	move_and_slide()

func detecting():
	anim.play("circle_detector")
	objects = detect_objects()
	await anim.animation_finished
	state = GRINDING

func grind():
	objects = detect_objects()
	if !objects.is_empty():
		anim.play("attack")
	else:
		nav_mesh.bake_navigation_polygon(true)
		state = IDLE
		print_debug(data.Inventory)

func farm():
	var damage: int = data.STRENCH
	var res: ActiveResourses = objects[0]
	data.add_item(InventoryItem.new(res.get_texture(), res.name, damage, res.type))
	res.get_damage(damage)
	
	if data.max_weight() <= data.total_resourse_amount():
		nav.target_position = storage.global_position
		state = GO_TO_STORAGE

func detect_objects():
	var targets: Array[Node2D] = trigger_area.get_overlapping_bodies()
	for body: ActiveResourses in targets:
		body.show_healthbar()
	return targets

func _on_mouse_entered():
	modulate = Color8(155, 155, 155, 255)
	mouse_enter = true

func _on_mouse_exited():
	modulate = Color8(255, 255, 255, 255)
	mouse_enter = false

func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("RightMouseButton"):
		if mouse_enter:
			SelectorClass.selected_object = self

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton"):
		if SelectorClass.selected_object == self:
			nav.target_position = get_global_mouse_position()
			state = SET_TARGET

func show_selected_info():
	return {
		"texture": anim_sprite.sprite_frames.get_frame_texture("idle", 0),
		"text": ("")
	}

func _to_string():
	return "Villager"
