extends Control

@onready var world_item: WorldListItem = preload("res://UI/prefabs/world_list_item.tscn").instantiate()

func RenderWorldList(worldsList: Array[GeneratorData]):
	for world: GeneratorData in worldsList:
		SetWorldAtList(world)

func SetWorldAtList(world: GeneratorData):
	var new_item: WorldListItem = world_item.duplicate()
	new_item.WorldDataPath = world.resource_path
	new_item.WorldPreview.texture = ImageTexture.create_from_image(world.BiomeMap)
	new_item.WorldSeed.value = world.seed
	new_item.WorldName.text = world.WorldName
	$MarginContainer2/VBoxContainer.add_child(new_item)


func _on_back_to_menu_button_down() -> void:
	visible = false
	var parent = get_parent()
	if (is_instance_valid(parent)):
		parent.get_node("MainButtons").visible = true
		
