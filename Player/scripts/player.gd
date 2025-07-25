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
@onready var concentration: ProgressBar = null #$Concentration

var MAP: WorldScene = null

enum {
	RUN,
	ATTACK,
	ITEM_ACTION,
}
var state = RUN

var current_active_item = null

var last_cell

func _ready():
	MAP = get_tree().root.get_node("Map")
	last_cell = Vector2(global_position / (64 * 8))

func _process(delta: float) -> void:
	await UpdateChunks()

func _physics_process(_delta):
	redrawGrid()
	match state:
		RUN:
			run()
		ITEM_ACTION:
			get_item_from_selected_HB_slot()
	
	move_and_slide()

func UpdateChunks():
	if (is_instance_valid(MAP)):
		MAP.UpdateChunkAroundPlayer(global_position)
	
func run():
	var direction = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down").normalized()
	if direction:
		velocity = direction * SPEED * AGILITY 
		animPlayer.play(getAnimByDirection(direction))
	else:
		velocity = Vector2(0, 0)
		animPlayer.play("idleStatic")

func getAnimByDirection(direction: Vector2):
	if direction == Vector2.DOWN:
		return "runDown"
	match direction:
		Vector2.DOWN:
			return "runDown"
		Vector2.UP:
			return "runUp"
		Vector2.LEFT:
			return "runLeft"
		Vector2.RIGHT:
			return "runRight"
		_:
			printerr("Непредвиденное направление")
			return "idleStatic"
	
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
	return anim.sprite_frames.get_frame_texture("idleStatic", 0)

var cell = Vector2(global_position / (64 * 8))

func redrawGrid():
	cell = Vector2(global_position / (64 * 8))
	if last_cell != cell:
		queue_redraw()
		last_cell = cell

var cellSize = Vector2(512, 512)
var range = 2
func drawGridAtCell():
	var world_offset = cell * cellSize - global_position.floor()
	for x in range(-range, range + 1):
		for y in range(-range, range + 1):
			var offset = Vector2(x, y) * cellSize
			var chunk_pos = world_offset + offset
			draw_rect(Rect2(chunk_pos, cellSize), Color.BLACK, false)
	last_cell = cell

func _on_hot_bar_selected_slot_changed(Item: InventoryItem):
	if current_active_item:
		remove_child(current_active_item)
		current_active_item = null

	if Item != null:
		current_active_item = Item
		Item.parent = self
		Item.setParametr("Agility", AGILITY)
		add_child(Item)
