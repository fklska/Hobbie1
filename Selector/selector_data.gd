extends Node
class_name SelectorDataClass

@export var texture: TextureRect
@export var VContainter: VBoxContainer

static var selected_obj: PhysicsBody2D
static var need_to_update: bool = false

static func set_selected_obj(obj: PhysicsBody2D):
	selected_obj = obj
	need_to_update = true
	print_debug("Setted: ", selected_obj)

func _process(_delta):
	if need_to_update:
		update_panel()
		need_to_update = false

func update_panel():
	clear()

	texture.texture = selected_obj.texture.texture
	var data: Dictionary = selected_obj.send_obj_data()
	
	for property in data:
		var label = Label.new()
		label.text = property + ": " + data[property]
		label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		VContainter.add_child(label)


func clear():
	texture.texture = null
	for label in VContainter.get_children():
		label.queue_free()
