extends Control
class_name InventoryUI

var display_flying_obj: Sprite2D
#var anim: AnimationPlayer

func _ready():
	display_flying_obj = get_node("FlyingObj")
	#anim = $"../AnimationPlayer"


func _process(delta):
	if Slot.flying_obj != null:
		display_flying_obj.texture = Slot.flying_obj.image
		display_flying_obj.get_child(0).text = str(Slot.flying_obj.amount)
		display_flying_obj.global_position = get_global_mouse_position()
	
	if disable_display:
		disable_display_fly_obj()


func update_slots():
	for i: Slot in InventoryData.inventory:
		if InventoryData.inventory[i] != null:
			i.update(InventoryData.inventory[i])
	#print_debug(InventoryData.inventory)

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
			#anim.play("appear")
		else:
			#anim.play("disapear")
			#await anim.animation_finished
			visible = false
