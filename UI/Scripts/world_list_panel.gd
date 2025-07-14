extends Control

@onready var world_item: WorldListItem = preload("res://UI/prefabs/world_list_item.tscn").instantiate()

func RenderWorldList(worldsList: Array[WorldScene]):
	for world: WorldScene in worldsList:
		SetWorldAtList(world)

func SetWorldAtList(world: WorldScene):
	var new_item: WorldListItem = world_item.duplicate()
	print_debug(world.WorldPreview, world.WorldSeed, world.WorldName)
	
	new_item.WorldPreview.texture = world.WorldPreview
	new_item.WorldSeed.value = world.WorldSeed
	new_item.WorldName.text = world.WorldName
	$VBoxContainer.add_child(new_item)
