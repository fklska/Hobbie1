extends CharacterBody2D
class_name Player

@export_range(10, 100) var AGILITY = 10
@export_range(10, 100) var STRENCH = 10
@export_range(10, 100) var INTELECT = 10

const SPEED = 50.0
@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var animPlayer = $AnimationPlayer
@onready var inv_ui: InventoryUI = $CanvasLayer/InventoryUI

#static var inventory: InvenoryData

enum {
	RUN,
	ATTACK,
}
var state = RUN

func _ready():
	pass

func _physics_process(delta):
	match state:
		RUN:
			run()
		ATTACK:
			attack()
	
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
		
	if Input.is_action_just_pressed("attack"):
		velocity = Vector2(0, 0)
		state = ATTACK
		
func attack():
	animPlayer.play("attack")
	await animPlayer.animation_finished
	state = RUN

func clearing():
	for obj: ActiveResourses in targets:
		var damage = STRENCH / 2
		add_item(obj.get_texture(), obj.name, damage, obj.type)
		obj.get_damage(damage)

var targets = []
func _on_area_2d_body_entered(body: StaticBody2D):
	targets.push_back(body)

func _on_area_2d_body_exited(body):
	targets.pop_back()

func add_item(_texture: Texture2D, _name: String, _amount: int, _type: String):
	#print_debug(InventoryData.inventory)
	for slot: Slot in InventoryData.inventory:
		var item: InventoryItem = InventoryData.inventory[slot]
		if not slot.is_empty():
			if item.image == _texture:
				item.amount += _amount
				inv_ui.update_slots()
				return "Updated!"

	for slot: Slot in InventoryData.inventory:
		if slot.is_empty():
			InventoryData.add_item(slot, InventoryItem.new(_texture, _name, _amount, _type))
			inv_ui.update_slots()
			return "Added"

func _input(event: InputEvent):
	if event.is_action_pressed("action"):
		#print_debug(InventoryData.inventory)
		if selected != null:
			selected.action(InventoryData.inventory)
			inv_ui.update_slots()

var selected: ActiveClass
func _on_trigger_body_entered(body):
	if body is Building:
		body.set_selected()
		selected = body


func _on_trigger_body_exited(body):
	if body is Building:
		body.hide_selected()
		selected = null


func show_selected_info():
	return {
		"texture": anim.sprite_frames.get_frame_texture("idle", 0),
		"text": ("   Stats    \n Agility: " + str(AGILITY) + 
		"\n Strench: " + str(STRENCH) + 
		"\n Intelect: " + str(INTELECT) + "\n")
	}


var mouse_enter: bool = false
func _on_mouse_entered():
	modulate = Color8(155, 155, 155, 255)
	mouse_enter = true

func _on_mouse_exited():
	modulate = Color8(255, 255, 255, 255)
	mouse_enter = false

func _on_input_event(viewport, event: InputEvent, shape_idx):
	if event.is_action_pressed("LeftMouseButton"):
		if mouse_enter:
			SelectorClass.selected_object = self
