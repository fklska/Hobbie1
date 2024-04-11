extends BaseVillager
class_name CollectorVillager

@export var searching_resourse_type = Globals.ActiveResoursesTypes.WOOD

@export var resourse_trigger: CollisionShape2D

var nearest_resourse: ActiveResourses

var start_collect: bool:
	set(value):
		start_collect = value
		$CheckButton.button_pressed = start_collect

func _physics_process(_delta):
	match state:
		RUN:
			run()
		ATTACK:
			attack()
		SEARCH:
			search()

func run():
	var dir = to_local(nav.get_next_path_position()).normalized()
	if dir:
		velocity = dir * SPEED * data.AGILITY
		anim_player.play("walk")
		if dir > Vector2(0, 0):
			anim.scale.x = -1
		
		if dir < Vector2(0, 0):
			anim.scale.x = 1

	if nav.is_navigation_finished():
		velocity = Vector2(0, 0)
		if start_collect:
			if nearest_resourse != null:
				state = ATTACK
		anim_player.play("idle")
	
	if enemy_target:
		nav.target_position = enemy_target.global_position
		var distance = global_position.distance_to(enemy_target.global_position)
		if distance <= 50:
			state = ATTACK

	if start_collect and nearest_resourse == null:
		state = SEARCH

	move_and_slide()

func search():
	if nearest_resourse == null:
		anim_player.play("search")

func attack():
	if nearest_resourse == null:
		await anim_player.animation_finished
		state = SEARCH
	anim_player.play("attack")

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton"):
		if RectSelectorClass.selected_object.has(self):
			start_collect = false
			nav.target_position = get_global_mouse_position()
			state = RUN
		

func _on_weapon_body_entered(body: ActiveResourses):
	if body.type == searching_resourse_type:
		var damage: int = 10 + data.STRENCH
		body.get_damage(damage)
		data.add_item(InventoryItem.new(body.get_texture(), body.name, damage, body.type))

func get_texture():
	return anim.sprite_frames.get_frame_texture("idle", 0)

func send_obj_data() -> Dictionary:
	return {
		"Basic": "Basic KinematicBody2dObject",
		"2nd": "Second label"
	}

func _on_resourse_trigger_body_entered(body: ActiveResourses):
	if body.type == searching_resourse_type:
		nearest_resourse = body
		nav.target_position = nearest_resourse.global_position
		resourse_trigger.scale = Vector2(1, 1)
		resourse_trigger.disabled = true
		state = RUN


func _on_check_button_pressed():
	start_collect = !start_collect
	if start_collect:
		nearest_resourse = null
		state = SEARCH
	else:
		state = RUN


func _on_animation_player_animation_finished(anim_name):
	if anim_name == "search":
		state = RUN
		start_collect = false
		print_debug("No res in visible area")
