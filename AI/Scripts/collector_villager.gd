extends BaseVillager

@export_enum("Wood", "Iron", "Stone", "Gold") var searching_resourse_type: String

@export var start_collect: bool
@export var resourse_trigger: CollisionShape2D

var nearest_resourse: ActiveResourses

enum {
	SEARCH = 6
}

func _physics_process(delta):
	match state:
		RUN:
			run()
		ATTACK:
			attack()
		SEARCH:
			search()

func run():
	#print_debug(start_collect)
	if !start_collect:
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
	else:
		print_debug(state)
		if nearest_resourse == null:
			state = SEARCH

	move_and_slide()

func search():
	if nearest_resourse == null:
		print_debug(anim.has_animation("search"))
		anim.play("search")

	if nav.is_navigation_finished():
		velocity = Vector2(0, 0)
		state = ATTACK

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton"):
		if SelectorClass.selected_object == self:
			start_collect = false
			nav.target_position = get_global_mouse_position()

func _on_weapon_body_entered(body: ActiveResourses):
	var damage: int = 10 + data.STRENCH
	body.get_damage(damage)
	data.add_item(InventoryItem.new(body.get_texture(), body.name, damage, body.type))
	

func _on_resourse_trigger_body_entered(body: ActiveResourses):
	print_debug(nearest_resourse)
	if body.type == searching_resourse_type:
		nearest_resourse = body
		nav.target_position = nearest_resourse.global_position
		resourse_trigger.scale = Vector2(1, 1)
		resourse_trigger.disabled = true
