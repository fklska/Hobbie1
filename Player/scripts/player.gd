extends CharacterBody2D
class_name Player

@export_range(10, 100) var AGILITY = 10
@export_range(10, 100) var STRENCH = 10
@export_range(10, 100) var INTELECT = 10


@export var selected_HB_item: TextureRect
@export var selected_HB_weapon_shape: CollisionShape2D

@export var INVENTORY: InventoryUI


const SPEED = 20.0
@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var animPlayer = $AnimationPlayer


enum {
	RUN,
	ATTACK,
	ITEM_ACTION,
}
var state = RUN

func _ready():
	pass

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

func add_item(_texture: Texture2D, _name: String, _amount: int, _type: String):
	for slot: Slot in INVENTORY.slots:
		if not slot.is_empty():
			var item: InventoryItem = slot.current_item
			if item._compare(_type):
				item.increase_amount(_amount)
				slot.update(item)
				return "Updated!"

	for slot: Slot in INVENTORY.slots:
		if slot.is_empty():
			slot.update(InventoryItem.new(_texture, _name, _amount, _type))
			return "Added"

func get_item_from_selected_HB_slot():
	var item = HotBarClass.current_selected_slot.current_item
	velocity = Vector2i.ZERO
	if item == null:
		state = RUN
		return
	
	if item is WeaponClass:
		animPlayer.play("item_action")
		var shape: CapsuleShape2D = CapsuleShape2D.new()
		shape.height = 40.0
		shape.radius = 3.0
		selected_HB_item.texture = item.image
		selected_HB_weapon_shape.shape = shape
		return
	
	if item is InventoryItem:
		selected_HB_item.texture = item.image
		selected_HB_weapon_shape.shape = null
		state = RUN
		return

func hide_item_from_hand():
	selected_HB_item.texture = null
	selected_HB_weapon_shape.shape = null

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

var mouse_enter: bool = false
func _on_mouse_entered():
	modulate = Color8(155, 155, 155, 255)
	mouse_enter = true

func _on_mouse_exited():
	modulate = Color8(255, 255, 255, 255)
	mouse_enter = false

func _on_weapon_body_entered(body):
	if body is ActiveResourses:
		var damage: int = 10 + STRENCH
		body.get_damage(damage)
		add_item(body.get_texture(), body.name, damage, body.type)
