extends StaticBodySelectedObject
class_name ActiveResourses

@export var HEALTH = 100
@export var STORAGE = 100
@export var type = Globals.ActiveResoursesTypes.EMPTY

@export var current_color = Color8(255, 255, 255, 255)

@export var damage_node: PackedScene


var type_name: Dictionary = {
	1: "Stone",
	2: "Wood",
	3: "Gold",
	4: "Iron"
}

func get_damage(damage: int):
	HEALTH -= damage
	current_color.a8 -= 5

	if HEALTH <= 0:
		#nav_mesh.remove_child(self)
		queue_free()

func poup(amount: String):
	var damage = damage_node.instantiate()
	damage.position = global_position
	damage.get_node("Label").text = amount
	get_tree().current_scene.add_child(damage)

func get_texture():
	return get_node("Texture").texture

func get_sprite():
	return get_node("Texture")

func show_selected_info():
	return {
		"texture": get_node("Texture").texture,
		"text": ("Resourse " + type_name.get(type) + "\n Health: " + str(HEALTH) + "\n Storage: " + str(STORAGE))
	}
	
func _to_string():
	return ("Resourse " + type_name.get(type) + "\n Health: " + str(HEALTH) + "\n Storage: " + str(STORAGE))


func _on_visible_on_screen_notifier_2d_screen_entered() -> void:
	process_mode = Node.PROCESS_MODE_INHERIT

func _on_visible_on_screen_notifier_2d_screen_exited() -> void:
	process_mode = Node.PROCESS_MODE_DISABLED
