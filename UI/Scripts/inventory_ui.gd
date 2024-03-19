extends Control
class_name InventoryUI

var slots: Array

var display_flying_obj: Sprite2D

func _ready():
	display_flying_obj = get_node("FlyingObj")
	slots = $GridContainer.get_children()

func _process(_delta):
	if Slot.flying_obj != null:
		display_flying_obj.texture = Slot.flying_obj.image
		display_flying_obj.get_child(0).text = str(Slot.flying_obj.amount)
		display_flying_obj.global_position = get_global_mouse_position()
	
	if disable_display:
		disable_display_fly_obj()

func update_slots():
	for i: Slot in slots:
		i.update(i.current_item)

static var disable_display: bool = false
func disable_display_fly_obj():
	display_flying_obj.texture = null
	display_flying_obj.get_child(0).text = ""
	display_flying_obj.global_position = Vector2(0, 0)
	disable_display = false


func _input(event: InputEvent):
	if event.is_action_pressed("inventory"):
		if !visible:
			visible = true
		else:
			visible = false
