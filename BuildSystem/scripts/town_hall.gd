extends StaticBodySelectedObject
class_name Building

@export var data: StorageDataClass
#@export var storage_ui: StorageUI

@export_category("BuildManager")
@export var Rectangle: Rect2
@export var RectColor: Color
@export var SelectRectColor: Color
@export var size_on_map: int = 1

var buildMode: BuildModeManager

var flying_obj: bool = false: 
	set(value):
		flying_obj = value
		queue_redraw()

func _process(delta):
	build()

func _ready():
	shader = texture.material
	buildMode = get_node("/root/BuildMode")

func start_build():
	flying_obj = true

func build():
	if (flying_obj):
		var world_pos: Vector2 = get_global_mouse_position()
		var coor = buildMode.cell2pixel((buildMode.pixel2cell(world_pos)))
		buildMode.size_multiplier = size_on_map
		global_position = coor

func _input(event: InputEvent):
	if event.is_action_pressed("LeftMouseButton"):
		flying_obj = false
		buildMode.build_mode = false

func action(inventory: Dictionary):
	for slot: Slot in inventory:
		if inventory[slot] != null:
			if inventory[slot] is InventoryItem:
				data.put_in_storage(inventory[slot])
				slot.clear_slot()
			
#	storage_ui.update_ui(data.data_items)

func send_obj_data() -> Dictionary:
	return {
		"Description": "Базовое строение в деревне"
	}

func get_texture():
	return get_node("Texture").texture

func create_human():
	print_debug("Villager Created!")

func _draw():
	if (flying_obj):
		draw_rect(Rectangle, RectColor, true)
	
