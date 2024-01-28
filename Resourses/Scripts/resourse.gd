extends StaticBody2D
class_name ActiveResourses

@export var HEALTH = 100
@export var STORAGE = 100
@export var type: String

@export var current_color = Color8(255, 255, 255, 255,)
func _on_mouse_entered():
	modulate = Color8(155, 155, 155)
	if Input.is_action_pressed("LeftMouseButton"):
		SelectorClass.selected_object = self


func _on_mouse_exited():
	modulate = current_color

		
@onready var anim = $AnimationPlayer
func get_damage():
	current_color.a8 -= 5
	anim.play("get_hit")
	update_healthbar()

	if HEALTH <= 0:
		queue_free()

@onready var hb = $healthbar
@onready var damage_bar = $damage_bar
@onready var timer: Timer = $damage_bar/Timer
func update_healthbar():
	hb.value = HEALTH
	timer.start()
	if hb.value == 100:
		hb.visible = false
		damage_bar.visible = false
	else:
		hb.visible = true
		damage_bar.visible = true

@export var damage_node: PackedScene
func poup(amount: String):
	var damage = damage_node.instantiate()
	damage.position = global_position
	damage.get_node("Label").text = amount
	get_tree().current_scene.add_child(damage)
	
func get_texture():
	return get_node("Sprite2D").texture

func _on_input_event(viewport, event: InputEvent, shape_idx):
	pass


func _on_timer_timeout():
	damage_bar.value = HEALTH


func show_selected_info():
	return [
		get_node("Sprite2D").texture,
		("Resourse " + type + "\n Health: " + str(HEALTH) + "\n Storage: " + str(STORAGE))
	]
