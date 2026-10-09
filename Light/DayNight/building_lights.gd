extends Node

const LAMP := preload("res://Light/DayNight/night_lamp.tscn")
const BUILDINGS_DIR := "res://BuildSystem/buildings/"
const HOST_GROUP := &"night_lamp_host"


func _ready() -> void:
	get_tree().node_added.connect(_on_node_added)
	for node in get_tree().root.find_children("*", "Node2D", true, false):
		_on_node_added(node)


func _on_node_added(node: Node) -> void:
	if node.scene_file_path.begins_with(BUILDINGS_DIR) or node.is_in_group(HOST_GROUP):
		_attach.call_deferred(node)


func _attach(node: Node) -> void:
	if not is_instance_valid(node) or not node is Node2D or node.has_node(^"NightLamp"):
		return
	if node is Building and not node.night_lamp:
		return
	var lamp: Node2D = LAMP.instantiate()
	lamp.name = &"NightLamp"
	var sprite := node.get_node_or_null(^"Texture") as Sprite2D
	if sprite:
		var rect := sprite.get_rect()
		lamp.position = sprite.transform * Vector2(rect.get_center().x, rect.end.y - rect.size.y * 0.1)
	node.add_child(lamp)
