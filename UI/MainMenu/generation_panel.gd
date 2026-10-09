extends Control

signal back_requested

const SIZES := [
	[128, "Малый", "128×128 тайлов, создаётся за несколько секунд."],
	[256, "Средний", "256×256 тайлов, создаётся около 20 секунд."],
	[384, "Большой", "384×384 тайла, создаётся около минуты."],
]

const STAGES := {
	"reset": "Подготовка карты",
	"BasicLandScapeStep": "Рельеф, климат и биомы",
	"CoastLineStep": "Береговая линия",
	"EnviromnetGenerationStep": "Леса и залежи руды",
	"save": "Сохранение мира",
	"scene": "Сборка сцены",
}

const LAYERS := ["BiomeMap", "HeightMap", "HeatMap", "MoistureMap"]

const NOUNS := [
	["долина", 1], ["край", 0], ["остров", 0], ["земля", 1], ["пустошь", 1], ["лес", 0],
	["берег", 0], ["холмы", 3], ["предгорье", 2], ["равнина", 1], ["чаща", 1], ["озёра", 3],
]
const ADJECTIVES := [
	["Туманный", "Туманная", "Туманное", "Туманные"],
	["Северный", "Северная", "Северное", "Северные"],
	["Тихий", "Тихая", "Тихое", "Тихие"],
	["Дикий", "Дикая", "Дикое", "Дикие"],
	["Забытый", "Забытая", "Забытое", "Забытые"],
	["Золотой", "Золотая", "Золотое", "Золотые"],
	["Мрачный", "Мрачная", "Мрачное", "Мрачные"],
	["Солнечный", "Солнечная", "Солнечное", "Солнечные"],
	["Каменный", "Каменная", "Каменное", "Каменные"],
	["Зелёный", "Зелёная", "Зелёное", "Зелёные"],
	["Древний", "Древняя", "Древнее", "Древние"],
	["Ветреный", "Ветреная", "Ветреное", "Ветреные"],
]

@onready var name_edit: LineEdit = %Name
@onready var seed_box: SpinBox = %Seed
@onready var size_buttons: Array[Button] = [%Small, %Medium, %Large]
@onready var size_hint: Label = %SizeHint
@onready var status: Label = %Status
@onready var map_view: TextureRect = %Map
@onready var placeholder: Label = %Placeholder
@onready var layer_buttons: Array[Button] = [%Biomes, %Height, %Heat, %Moisture]
@onready var generate_button: Button = %Generate
@onready var play_button: Button = %Play

var size_index := 1
var busy := false
var layers: Array[Texture2D] = []
var created_world := ""


func _ready() -> void:
	var size_group := ButtonGroup.new()
	for i in size_buttons.size():
		size_buttons[i].button_group = size_group
		size_buttons[i].pressed.connect(select_size.bind(i))
	var layer_group := ButtonGroup.new()
	for i in layer_buttons.size():
		layer_buttons[i].button_group = layer_group
		layer_buttons[i].pressed.connect(show_layer.bind(i))
	%RandomName.pressed.connect(func() -> void: name_edit.text = random_name())
	%RandomSeed.pressed.connect(func() -> void: seed_box.value = randi_range(0, 2147483646))
	%Back.pressed.connect(func() -> void: if not busy: back_requested.emit())
	generate_button.pressed.connect(generate)
	play_button.pressed.connect(play)
	GENERATOR.StageChanged.connect(_on_stage_changed)
	select_size(size_index)


func open() -> void:
	show()
	reset_preview()
	name_edit.text = random_name()
	seed_box.value = randi_range(0, 2147483646)
	status.text = ""
	generate_button.grab_focus.call_deferred()


func is_busy() -> bool:
	return busy


func select_size(index: int) -> void:
	size_index = index
	for i in size_buttons.size():
		size_buttons[i].set_pressed_no_signal(i == index)
	size_hint.text = SIZES[index][2]


func reset_preview() -> void:
	layers.clear()
	created_world = ""
	map_view.texture = null
	placeholder.show()
	for button in layer_buttons:
		button.disabled = true
		button.set_pressed_no_signal(false)
	play_button.hide()
	generate_button.text = "Создать мир"
	generate_button.theme_type_variation = &"PrimaryButton"


func show_layer(index: int) -> void:
	if index >= layers.size():
		return
	map_view.texture = layers[index]
	for i in layer_buttons.size():
		layer_buttons[i].set_pressed_no_signal(i == index)


func random_name() -> String:
	var noun: Array = NOUNS.pick_random()
	var adjective: Array = ADJECTIVES.pick_random()
	return "%s %s" % [adjective[noun[1]], noun[0]]


func unique_name(raw: String) -> String:
	var base := raw.strip_edges().validate_filename().replace("__SIMPLE", "")
	if base == "":
		base = random_name()
	var result := base
	var index := 2
	while WorldStore.exists(result):
		result = "%s %d" % [base, index]
		index += 1
	return result


func generate() -> void:
	if busy:
		return
	busy = true
	reset_preview()
	var world_name := unique_name(name_edit.text)
	name_edit.text = world_name
	Overlay.loading.open("Создание мира «%s»" % world_name, STAGES["reset"])
	await get_tree().process_frame
	GENERATOR.StartGeneration(Overlay.loading.bar, world_name, int(seed_box.value), SIZES[size_index][0])
	var result: Array = await GENERATOR.GenerationFinished
	Overlay.loading.close()
	busy = false
	if not result[0]:
		status.text = "Не удалось создать мир. Подробности в логе."
		return
	created_world = result[1]
	var data = GENERATOR.genData
	for layer in LAYERS:
		layers.append(ImageTexture.create_from_image(data.get(layer)))
	for button in layer_buttons:
		button.disabled = false
	placeholder.hide()
	show_layer(0)
	status.text = "Мир «%s» готов." % created_world
	generate_button.text = "Создать ещё"
	generate_button.theme_type_variation = &""
	play_button.show()
	play_button.grab_focus()


func play() -> void:
	var world := WorldStore.find(WorldStore.DIR + created_world + ".tscn")
	if world != null:
		WorldStore.play(world, true)


func _on_stage_changed(stage: String, _index: int, _count: int) -> void:
	Overlay.loading.set_stage(STAGES.get(stage, "Генерация"))
