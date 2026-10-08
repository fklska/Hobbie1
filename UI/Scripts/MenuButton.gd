extends Button

@export var target: NodePath = ^"../BuildMenu"


func _on_pressed():
	var node := get_node_or_null(target) as CanvasItem
	if node:
		node.visible = not node.visible
