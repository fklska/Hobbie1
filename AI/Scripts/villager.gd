extends CharacterBody2D
class_name VillagerClass

@export var data: AIBackData
@export var circle_detector: Panel

@onready var nav: NavigationAgent2D = $NavigationAgent2D
@onready var nav_mesh: NavigationRegion2D = $"../NavigationRegion2D"

@onready var anim: AnimationPlayer = $AnimationPlayer
@onready var anim_sprite: AnimatedSprite2D = $AnimatedSprite2D

@onready var trigger_area: Area2D = $Area2D

const SPEED = 50

enum {
	IDLE,
	RUN,
	GRINDING,
	SET_TARGET
}

var state = IDLE

var mouse_enter: bool = false

func _physics_process(delta):
	match state:
		IDLE:
			idle()
		RUN:
			run()
		GRINDING:
			grind()
		SET_TARGET:
			run()

func idle():
	anim.play("idle")
	
	if Input.is_action_just_pressed("LeftMouseButton"):
		if SelectorClass.selected_object == self:
			nav.target_position = get_global_mouse_position()
			state = SET_TARGET

func run():
	var dir = to_local(nav.get_next_path_position()).normalized()
	velocity = dir * SPEED * data.AGILITY
	
	if Input.is_action_just_pressed("LeftMouseButton"):
		if SelectorClass.selected_object == self:
			nav.target_position = get_global_mouse_position()
			state = SET_TARGET

	if nav.is_navigation_finished():
		velocity = Vector2(0, 0)
		if state == SET_TARGET:
			anim.play("circle_detector")
			await anim.animation_finished
			state = GRINDING

	move_and_slide()

func grind():
	anim.play("attack")

func farm():
	var objects: Array[Node2D] = detect_objects()
	if !objects.is_empty():
		var damage: int = data.STRENCH
		var res: ActiveResourses = objects[0]
		data.add_item(InventoryItem.new(res.get_texture(), res.name, damage, res.type))
		res.get_damage(damage)
	else:
		nav_mesh.bake_navigation_polygon(true)
		state = IDLE
		print_debug(data.Inventory)

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
			
func show_selected_info():
	return {
		"texture": anim_sprite.sprite_frames.get_frame_texture("idle", 0),
		"text": ("")
	}

func _to_string():
	return "Villager"
