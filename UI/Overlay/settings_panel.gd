extends Control

signal closed

@onready var tab_buttons: Array[Button] = [%SoundTab, %VideoTab, %ControlsTab]
@onready var pages: Array[Control] = [%Sound, %Video, %Controls]
@onready var fullscreen: CheckButton = %Fullscreen
@onready var vsync: CheckButton = %VSync
@onready var show_fps: CheckButton = %ShowFps
@onready var fps_option: OptionButton = %FpsOption
@onready var scale_option: OptionButton = %ScaleOption
@onready var controls_list: VBoxContainer = %ControlsList
@onready var reset_button: Button = %Reset

var waiting_action := ""
var key_buttons := {}


func _ready() -> void:
	hide()
	var group := ButtonGroup.new()
	for i in tab_buttons.size():
		tab_buttons[i].button_group = group
		tab_buttons[i].pressed.connect(select_tab.bind(i))
	build_audio()
	build_video()
	build_controls()
	reset_button.pressed.connect(_on_reset)
	%Done.pressed.connect(close)
	select_tab(0)


func open() -> void:
	refresh_video()
	refresh_controls()
	show()
	%Done.grab_focus.call_deferred()


func close() -> void:
	waiting_action = ""
	refresh_controls()
	hide()
	closed.emit()


func select_tab(index: int) -> void:
	for i in pages.size():
		pages[i].visible = i == index
		tab_buttons[i].set_pressed_no_signal(i == index)
	reset_button.visible = index == 2


func build_audio() -> void:
	for pair in GameSettings.AUDIO_BUSES:
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 16)
		var label := Label.new()
		label.text = pair[1]
		label.custom_minimum_size.x = 200
		var slider := HSlider.new()
		slider.max_value = 1.0
		slider.step = 0.05
		slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		slider.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		slider.value = GameSettings.get_value("audio", pair[0], 0.8)
		var value := Label.new()
		value.custom_minimum_size.x = 56
		value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		value.theme_type_variation = &"ValueLabel"
		value.text = "%d%%" % roundi(slider.value * 100)
		slider.value_changed.connect(func(v: float) -> void:
			value.text = "%d%%" % roundi(v * 100)
			GameSettings.set_value("audio", pair[0], v))
		row.add_child(label)
		row.add_child(slider)
		row.add_child(value)
		row.visible = GameSettings.has_bus(pair[0])
		%Sound.add_child(row)


func build_video() -> void:
	for limit in GameSettings.FPS_LIMITS:
		fps_option.add_item("Без ограничения" if limit == 0 else str(limit))
	for value in GameSettings.SCALES:
		scale_option.add_item("Авто" if value == 0.0 else "%d%%" % roundi(value * 100))
	fullscreen.toggled.connect(func(on: bool) -> void: GameSettings.set_value("video", "fullscreen", on))
	vsync.toggled.connect(func(on: bool) -> void: GameSettings.set_value("video", "vsync", on))
	show_fps.toggled.connect(func(on: bool) -> void: GameSettings.set_value("video", "show_fps", on))
	fps_option.item_selected.connect(func(i: int) -> void: GameSettings.set_value("video", "max_fps", GameSettings.FPS_LIMITS[i]))
	scale_option.item_selected.connect(func(i: int) -> void: GameSettings.set_value("video", "scale", GameSettings.SCALES[i]))


func refresh_video() -> void:
	fullscreen.set_pressed_no_signal(GameSettings.get_value("video", "fullscreen", false))
	vsync.set_pressed_no_signal(GameSettings.get_value("video", "vsync", true))
	show_fps.set_pressed_no_signal(GameSettings.get_value("video", "show_fps", false))
	fps_option.select(maxi(0, GameSettings.FPS_LIMITS.find(GameSettings.get_value("video", "max_fps", 0))))
	scale_option.select(maxi(0, GameSettings.SCALES.find(GameSettings.get_value("video", "scale", 0.0))))


func build_controls() -> void:
	for pair in GameSettings.ACTIONS:
		var row := HBoxContainer.new()
		var label := Label.new()
		label.text = pair[1]
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		var button := Button.new()
		button.custom_minimum_size.x = 190
		button.pressed.connect(_start_rebind.bind(pair[0]))
		row.add_child(label)
		row.add_child(button)
		controls_list.add_child(row)
		key_buttons[pair[0]] = button
	refresh_controls()


func refresh_controls() -> void:
	for action in key_buttons:
		var button: Button = key_buttons[action]
		button.text = "Нажми клавишу" if action == waiting_action else GameSettings.key_name(action)
		button.theme_type_variation = &"PrimaryButton" if action == waiting_action else &""


func _start_rebind(action: String) -> void:
	waiting_action = action
	refresh_controls()


func _on_reset() -> void:
	waiting_action = ""
	GameSettings.reset_controls()
	refresh_controls()


func _input(event: InputEvent) -> void:
	if not visible or waiting_action == "":
		return
	var key := event as InputEventKey
	if key == null or not key.pressed or key.echo:
		return
	if key.physical_keycode != KEY_ESCAPE:
		GameSettings.rebind(waiting_action, key)
	waiting_action = ""
	refresh_controls()
	get_viewport().set_input_as_handled()
