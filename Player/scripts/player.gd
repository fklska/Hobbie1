extends CharacterBody2D
class_name Player

@export_range(10, 100) var AGILITY = 10
@export_range(10, 100) var STRENCH = 10
@export_range(10, 100) var INTELECT = 10

const SPEED = 50.0
@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var animPlayer = $AnimationPlayer
@onready var inv_ui: InventoryUI = $InventoryUI

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
		var damage = STRENCH / 10
		print_debug(add_item(obj.get_texture(), obj.name, damage))
		obj.HEALTH -= damage
		obj.get_damage()
		obj.poup(str(damage))

var targets = []
func _on_area_2d_body_entered(body: StaticBody2D):
	targets.push_back(body)

func _on_area_2d_body_exited(body):
	targets.pop_back()

func add_item(_texture: Texture2D, _name: String, _amount: int):
	print_debug(InventoryData.inventory)
	for i: Slot in InventoryData.inventory:
		print_debug("Start 1 cycle")
		var item: InventoryItem = InventoryData.inventory[i]
		if not i.is_empty():
			if item.image == _texture:
				item.amount += _amount
				inv_ui.update_slots()
				return "Updated!"

	for i: Slot in InventoryData.inventory:
		print_debug("Start 2 cycle")
		if i.is_empty():
			InventoryData.inventory[i] = InventoryItem.new(_texture, _name, _amount)
			inv_ui.update_slots()
			return "Added"
	
