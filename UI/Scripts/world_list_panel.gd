extends Control

@onready var world_item: WorldListItem = preload("res://UI/prefabs/world_list_item.tscn").instantiate()

func RenderWorldList(worldsList: Array[SimpleGeneratorData]):
	for world: SimpleGeneratorData in worldsList:
		SetWorldAtList(world)

func SetWorldAtList(world: SimpleGeneratorData):
	var new_item: WorldListItem = world_item.duplicate()
	new_item.WorldData = world
	new_item.WorldDataPath = world.fullDataPath
	new_item.WorldPreview.texture = ImageTexture.create_from_image(world.BiomeMap)
	new_item.WorldSeed.value = world.seed
	new_item.WorldName.text = world.WorldName
	$MarginContainer2/VBoxContainer.add_child(new_item)


func _on_back_to_menu_button_down() -> void:
	visible = false
	var parent = get_parent()
	if (is_instance_valid(parent)):
		parent.get_node("MainButtons").visible = true
		
