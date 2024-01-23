extends StaticBody2D
class_name ActiveResourses

@export var HEALTH = 100
@export var STORAGE = 100
# Called when the node enters the scene tree for the first time.
func _ready():
	#name = "Iron" # Replace with function body.
	pass

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

@export var current_color = Color8(255, 255, 255, 255,)
func _on_mouse_entered():
	modulate = Color8(155, 155, 155) # Replace with function body.


func _on_mouse_exited():
	modulate = current_color # Replace with function body.

		
@onready var anim = $AnimationPlayer
func get_damage():
	current_color.a8 -= 5
	anim.play("get_hit")
	update_healthbar()

	if HEALTH <= 0:
		queue_free()

@onready var hb = $healthbar
func update_healthbar():
	hb.value = HEALTH

	if hb.value == 100:
		hb.visible = false
	else:
		hb.visible = true

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
