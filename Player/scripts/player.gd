extends KinematicBodyEntity
class_name Player

@export_range(10, 100) var AGILITY = 10
@export_range(10, 100) var STRENCH = 10
@export_range(10, 100) var INTELECT = 10
@export_range(10, 100) var CONCENTRATION = 10

@export var INVENTORY: InventoryUI
@export var Hotbar: HotBarClass
@export var enableConcentration: bool

const SPEED = 20.0
@onready var animPlayer: AnimationPlayer = $AnimationPlayer
@onready var concentration: ProgressBar = $Concentration

enum {
	RUN,
	ATTACK,
	ITEM_ACTION,
}
var state = RUN

func _physics_process(_delta):
	match state:
		RUN:
			run()
		ITEM_ACTION:
			get_item_from_selected_HB_slot()
	
	move_and_slide()

func run():
	var direction = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down").normalized()
	
	if direction:
		velocity = direction * SPEED * AGILITY 
		animPlayer.play("run")
		
		if direction > Vector2(0, 0):
			anim.scale.x = -1
		
		if direction < Vector2(0, 0):
			anim.scale.x = 1
	else:
		velocity = Vector2(0, 0)
		animPlayer.play("idle")

func add_item(_texture: Texture2D, _amount: int, _type: int):
	for slot: Slot in INVENTORY.slots:
		if not slot.is_empty():
			var item: InventoryItem = slot.current_item
			if item._compare(_type):
				item.increase_amount(_amount)
				slot.update(item)
				return "Updated!"

	for slot: Slot in INVENTORY.slots:
		if slot.is_empty():
			var item = InventoryItem.new()
			item.set_properties(_texture, _amount, _type)
			slot.update(item)
			return "Added"

func get_item_from_selected_HB_slot():
	var item = HotBarClass.current_selected_slot.current_item
	velocity = Vector2i.ZERO
	if item == null:
		state = RUN
		return
	
	if item is WeaponClass:
		state = RUN
	
	if item is InventoryItem:
		pass

func hide_item_from_hand():
	pass

func calculate_damage():
	if enableConcentration:
		print_debug(STRENCH * concentration.conc_value / 100)
		return STRENCH * concentration.conc_value / 100
	return STRENCH / 100

func _input(event: InputEvent):
	if INVENTORY.visible == false:
		if event.is_action_pressed("LeftMouseButton"):
			state = ITEM_ACTION
	
	if event.is_action_released("LeftMouseButton"):
		if animPlayer.current_animation == "item_action":
			await animPlayer.animation_finished
		hide_item_from_hand()
		state = RUN

func show_selected_info():
	return {
		"texture": anim.sprite_frames.get_frame_texture("idle", 0),
		"text": ("   Stats    \n Agility: " + str(AGILITY) + 
		"\n Strench: " + str(STRENCH) + 
		"\n Intelect: " + str(INTELECT) + "\n")
	}

func get_texture():
	return anim.sprite_frames.get_frame_texture("idle", 0)

#func _on_weapon_body_entered(body):
	#if body is ActiveResourses:
		#var damage: int = 10 + STRENCH
		#body.get_damage(damage)
		#add_item(body.get_texture(), damage, body.type)

func _on_hot_bar_selected_slot_changed(Item: InventoryItem):
	if is_instance_valid(Item):
		Item.set_selected({"Parent": self})
