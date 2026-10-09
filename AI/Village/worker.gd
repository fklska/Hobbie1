extends KinematicBodyEntity
class_name Worker

signal died

const SPEED := 160.0
const ROT_SPEED := 3.0
const ACCELERATION := 8.0
const COLLECT_TIME := 2.94
const AREA_SIZE := 3840.0
const DEPOSIT_RANGE := 80.0

@export var max_hp := 40

@onready var ai: WorkerController = $AIController2D
@onready var tool: Sprite2D = $AnimatedSprite2D/Tool
@onready var carry_label: Label = $Carry
@onready var combat: VillagerCombat = $Combat

var hp: int
var act := 4
var heading := 0.0
var current_velocity := Vector2.ZERO
var on_cell := false
var touched_wall := false
var dwell := 0.0
var dwell_cell := Vector2i(-1, -1)
var carrying_kind := ""
var carrying_amount := 0
var weapon_id := ""

func _ready():
	super()
	hp = max_hp
	heading = randf() * TAU
	add_to_group("village")
	ai.init(self)

static func cell_center(cell: Vector2i) -> Vector2:
	return Vector2(cell * 64) + Vector2(32, 32)

func hall() -> Building:
	var town_hall = Game.TownHall
	return town_hall if is_instance_valid(town_hall) else null

func area() -> Rect2:
	var town_hall := hall()
	var center := town_hall.get_center() if town_hall else global_position
	return Rect2(center - Vector2.ONE * AREA_SIZE / 2, Vector2.ONE * AREA_SIZE)

func is_carrying() -> bool:
	return carrying_amount > 0

func _physics_process(delta: float):
	if ai.needs_reset and ai.heuristic == "model":
		reset_episode()
		return
	if combat.update(delta):
		keep_off_ocean(delta)
		move_and_slide()
		_animate()
		return
	var world = Game.World
	var town_hall := hall()
	if world == null or town_hall == null:
		velocity = Vector2.ZERO
		_animate()
		return

	if is_carrying():
		_return_to(town_hall)
	else:
		_gather(world, delta)
	keep_off_ocean(delta)
	move_and_slide()
	_animate()

func _gather(world, delta: float):
	var move_dir := 0.0
	var rotate_dir := 0.0
	match act:
		0: move_dir = 1.0
		1: move_dir = -1.0
		2: rotate_dir = 1.0
		3: rotate_dir = -1.0
	heading = wrapf(heading + rotate_dir * ROT_SPEED * delta, -PI, PI)
	current_velocity = current_velocity.lerp(Vector2.from_angle(heading) * move_dir * SPEED, ACCELERATION * delta)
	velocity = current_velocity
	_keep_in_area(delta)

	var cell := Vector2i((global_position / 64).floor())
	on_cell = Game.GetKindOf(world.GetResourceAt(cell)) == Game.WorkerJob
	if not on_cell:
		dwell = 0.0
		tool.rotation = 0
		return
	if cell != dwell_cell:
		dwell_cell = cell
		dwell = 0.0
	dwell += delta
	tool.rotation = sin(dwell * 12.0) * 1.2
	if dwell >= COLLECT_TIME / Game.WorkSpeed:
		_collect(world, cell)

func _keep_in_area(delta: float):
	var bounds := area()
	var next := global_position + velocity * delta
	var clamped := next.clamp(bounds.position, bounds.end)
	if clamped != next:
		touched_wall = true
		velocity = (clamped - global_position) / delta
		current_velocity = Vector2.ZERO

func _collect(world, cell: Vector2i):
	dwell = 0.0
	tool.rotation = 0
	var type = world.HarvestTile(cell)
	if type == 0:
		return
	Sound.PlayAt(Sound.HarvestSound(type), global_position)
	carrying_kind = Game.GetKindOf(type)
	carrying_amount = Game.GetYieldOf(type)
	carry_label.text = "+%d" % carrying_amount
	carry_label.visible = true
	on_cell = false
	ai.on_collect()

func _return_to(town_hall: Building):
	var to_hall := town_hall.get_center() - global_position
	if to_hall.length() <= DEPOSIT_RANGE:
		velocity = Vector2.ZERO
		_deposit()
		return
	var direction := steer_to(town_hall.get_center())
	heading = direction.angle()
	current_velocity = direction * SPEED
	velocity = current_velocity

func _deposit():
	Game.AddResource(carrying_kind, carrying_amount)
	carrying_kind = ""
	carrying_amount = 0
	carry_label.visible = false

func _animate():
	if velocity.length() > 5:
		anim.play("walk")
		anim.flip_h = velocity.x > 0
	else:
		anim.play("idle")

func take_damage(amount: int):
	if hp <= 0:
		return
	hp -= amount
	modulate = Color(1, 0.5, 0.5)
	create_tween().tween_property(self, "modulate", Color.WHITE, 0.2)
	if hp <= 0:
		if not weapon_id.is_empty():
			Game.ReturnWeapon(weapon_id)
		died.emit()
		queue_free()

func equip(id: String, stats: Dictionary):
	weapon_id = id
	combat.equip(stats)

func reset_episode():
	var town_hall := hall()
	if town_hall:
		global_position = town_hall.get_center() + Vector2.from_angle(randf() * TAU) * randf_range(80.0, 200.0)
	heading = randf() * TAU
	current_velocity = Vector2.ZERO
	act = 4
	on_cell = false
	touched_wall = false
	dwell = 0.0
	carrying_amount = 0
	carrying_kind = ""
	carry_label.visible = false
	tool.rotation = 0
	ai.reset()
	ai.done = true

func get_texture():
	return anim.sprite_frames.get_frame_texture("idle", 0)

func send_obj_data() -> Dictionary:
	return {
		"Description": "Житель-сборщик",
		"Weapon": combat.stats.get("title", "нет"),
		"HP": "%d/%d" % [hp, max_hp]
	}
