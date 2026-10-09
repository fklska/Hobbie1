extends PanelContainer
class_name WorldListItem

signal play_requested(world: SimpleGeneratorData, fresh: bool)
signal delete_requested(world: SimpleGeneratorData)

const SIZE_NAMES := {128: "Малый", 256: "Средний", 384: "Большой"}

@onready var preview: TextureRect = %Preview
@onready var world_name: Label = %WorldName
@onready var details: Label = %Details
@onready var run_info: Label = %RunInfo
@onready var play_button: Button = %Play
@onready var new_run_button: Button = %NewRun

var world: SimpleGeneratorData


func setup(data: SimpleGeneratorData) -> void:
	world = data
	if data.BiomeMap != null:
		preview.texture = ImageTexture.create_from_image(data.BiomeMap)
	world_name.text = data.WorldName
	var size_name: String = SIZE_NAMES.get(data.mapSize.x, "Мир")
	var date := Time.get_datetime_dict_from_unix_time(WorldStore.modified_time(data))
	details.text = "%s %d×%d · сид %d · создан %02d.%02d.%d" % [size_name, data.mapSize.x, data.mapSize.y, data.seed, date.day, date.month, date.year]
	var has_run := WorldStore.has_run(data)
	var day := WorldStore.run_day(data)
	run_info.visible = has_run
	run_info.text = "Идёт партия: день %d из 30" % day if day > 0 else "Есть сохранённая партия"
	play_button.text = "Продолжить" if has_run else "Играть"
	new_run_button.visible = has_run
	play_button.pressed.connect(func() -> void: play_requested.emit(world, false))
	new_run_button.pressed.connect(func() -> void: play_requested.emit(world, true))
	%Delete.pressed.connect(func() -> void: delete_requested.emit(world))
