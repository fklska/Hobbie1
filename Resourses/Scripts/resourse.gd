extends StaticBodySelectedObject
class_name ActiveResourses

@export var HEALTH = 100
@export var STORAGE = 100
@export_enum("Stone", "Gold", "Wood", "Iron") var type: String

@export var current_color = Color8(255, 255, 255, 255)

@export var damage_node: PackedScene

@onready var hb = $healthbar
@onready var damage_bar = $damage_bar
@onready var timer: Timer = $damage_bar/Timer
@onready var anim = $AnimationPlayer


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

func _on_timer_timeout():
	damage_bar.value = HEALTH

func show_selected_info():
	return {
		"texture": get_node("Sprite2D").texture,
		"text": ("Resourse " + type + "\n Health: " + str(HEALTH) + "\n Storage: " + str(STORAGE))
	}

func _to_string():
	return ("Resourse " + type + "\n Health: " + str(HEALTH) + "\n Storage: " + str(STORAGE))
