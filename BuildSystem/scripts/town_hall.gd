extends StaticBodySelectedObject
class_name Building

@export var data: StorageDataClass
@onready var storage_ui: StorageUI = $CanvasLayer2/storage_ui
@onready var texture: Sprite2D = $Texture

var shader: ShaderMaterial

func _ready():
	shader = texture.material
	
func action(inventory: Dictionary):
	for slot: Slot in inventory:
		if inventory[slot] != null:
			if inventory[slot] is InventoryItem:
				data.put_in_storage(inventory[slot])
				slot.clear_slot()
			
	storage_ui.update_ui(data.data_items)

func create_human():
	print_debug("Villager Created!")

func show_selected_info():
	return {
		"texture": get_node("Sprite2D").texture,
		"text": "Building",
		"action": [create_human]
		}

func set_outline():
	shader.set_shader_parameter("enable", true)
	
func hide_outline():
	shader.set_shader_parameter("enable", false)

func _on_mouse_entered():
	set_outline()


func _on_mouse_exited():
	hide_outline()
