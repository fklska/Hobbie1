extends CharacterBody2D
class_name Player

@export_range(10, 100) var AGILITY = 10
@export_range(10, 100) var STRENCH = 10
@export_range(10, 100) var INTELECT = 10

const SPEED = 50.0
@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var animPlayer = $AnimationPlayer
@onready var inv_ui: InventoryUI = $CanvasLayer/InventoryUI

#static var inventory: InventoryData

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
	var direction = Input.get_axis("ui_left", "ui_right")
	var direction_y = Input.get_axis("ui_up", "ui_down")
	
	if direction or direction_y and state == RUN:
		velocity.y = direction_y * SPEED * AGILITY / 2
		velocity.x = direction * SPEED * AGILITY 
		animPlayer.play("run")
		
		if direction > 0:
			anim.scale.x = -1
		
		if direction < 0:
			anim.scale.x = 1
	else:
		velocity.y = move_toward(velocity.y, 0, SPEED)
		velocity.x = move_toward(velocity.x, 0, SPEED)
		animPlayer.play("idle")
		
	if Input.is_action_just_pressed("attack"):
		state = ATTACK
		
func attack():
	animPlayer.play("attack")
	await animPlayer.animation_finished
	state = RUN

func clearing():
	for obj: ActiveResourses in targets:
		var damage = STRENCH / 2
		add_item(obj.get_texture(), obj.name, damage)
		obj.HEALTH -= damage
		obj.get_damage()
		obj.poup(str(damage))

var targets = []
func _on_area_2d_body_entered(body: StaticBody2D):
	targets.push_back(body)

func _on_area_2d_body_exited(body):
	targets.pop_back()

func add_item(_texture: Texture2D, _name: String, _amount: int):
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
			InventoryData.add_item(slot, InventoryItem.new(_texture, _name, _amount))
			inv_ui.update_slots()
			return "Added"


func _on_trigger_body_entered(body):
	body.set_selected()
	# Квадратный селектор
	# Буковка е над обектом
	# В инпуте сделать если нажал е рядом с активным объектом то вызывай его функцию взаимодействия
	# Replace with function body.


func _on_trigger_body_exited(body):
	body.hide_selected()
