extends StaticBody2D
class_name ActiveResourses

@export var HEALTH = 100
@export var STORAGE = 100
@export var type: String = "Resourse"

@export var current_color = Color8(255, 255, 255, 255)

@export var damage_node: PackedScene

@onready var hb = $healthbar
@onready var damage_bar = $damage_bar
@onready var timer: Timer = $damage_bar/Timer
@onready var anim = $AnimationPlayer

var mouse_enter: bool = false
@onready var nav_mesh: NavigationRegion2D = get_parent()

func _on_mouse_entered():
	modulate = Color8(155, 155, 155, 255)
	mouse_enter = true

func _on_mouse_exited():
	modulate = current_color
	mouse_enter = false

func get_damage(damage: int):
	HEALTH -= damage
	current_color.a8 -= 5
	anim.play("get_hit")
	poup(str(damage))
	update_healthbar()

	if HEALTH <= 0:
		#nav_mesh.remove_child(self)
		queue_free()

func update_healthbar():
	hb.value = HEALTH
	timer.start()
	if hb.value == 100:
		hb.visible = false
		damage_bar.visible = false
	else:
		hb.visible = true
		damage_bar.visible = true

func poup(amount: String):
	var damage = damage_node.instantiate()
	damage.position = global_position
	damage.get_node("Label").text = amount
	get_tree().current_scene.add_child(damage)

func get_texture():
	return get_node("Sprite2D").texture

func show_healthbar():
	hb.visible = true

func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("LeftMouseButton"):
		if mouse_enter:
			SelectorClass.selected_object = self

func _on_timer_timeout():
	damage_bar.value = HEALTH

func show_selected_info():
	return {
		"texture": get_node("Sprite2D").texture,
		"text": ("Resourse " + type + "\n Health: " + str(HEALTH) + "\n Storage: " + str(STORAGE))
	}

func _to_string():
	return ("Resourse " + type + "\n Health: " + str(HEALTH) + "\n Storage: " + str(STORAGE))
