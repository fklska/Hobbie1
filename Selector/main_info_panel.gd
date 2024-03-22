extends MarginContainer

@export var texture: TextureRect
@export var VContainter: VBoxContainer


func update_panel():
	clear()
	
	var obj = SelectorDataClass.selected_obj as StaticBodySelectedObject
	texture.texture = obj.texture.texture
	var data: Dictionary = obj.send_obj_data()
	
	for property in data:
		var label = Label.new()
		label.text = property + ": " + data[property]
		label.size = Vector2(96, 32)
		VContainter.add_child(label)


func clear():
	texture.texture = null
	for label in VContainter.get_children():
		label.queue_free()
