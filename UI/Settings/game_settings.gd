class_name GameSettings
extends RefCounted

const PATH := "user://settings.cfg"

const AUDIO_BUSES := [["Master", "Общая громкость"], ["Music", "Музыка"], ["SFX", "Эффекты"]]

const ACTIONS := [
	["ui_up", "Вверх"],
	["ui_down", "Вниз"],
	["ui_left", "Влево"],
	["ui_right", "Вправо"],
	["action", "Действие"],
	["attack", "Атака"],
	["skill_1", "Способность 1"],
	["skill_2", "Способность 2"],
	["skill_3", "Способность 3"],
	["skills", "Дерево способностей"],
	["inventory", "Инвентарь"],
	["menu", "Строительство"],
	["zoom+", "Приблизить"],
	["zoom-", "Отдалить"],
]

const FPS_LIMITS := [0, 30, 60, 120, 144, 240]
const SCALES := [0.0, 1.0, 1.25, 1.5, 1.75, 2.0]

static var config := ConfigFile.new()
static var _defaults := {}


static func load_and_apply() -> void:
	config.load(PATH)
	for pair in ACTIONS:
		_defaults[pair[0]] = InputMap.action_get_events(pair[0])
	apply_all()


static func save() -> void:
	config.save(PATH)


static func get_value(section: String, key: String, default: Variant) -> Variant:
	return config.get_value(section, key, default)


static func set_value(section: String, key: String, value: Variant) -> void:
	config.set_value(section, key, value)
	match section:
		"audio":
			apply_audio()
		"video":
			apply_video()
	save()


static func apply_all() -> void:
	apply_audio()
	apply_video()
	apply_controls()


static func has_bus(bus: String) -> bool:
	return AudioServer.get_bus_index(bus) != -1


static func apply_audio() -> void:
	for pair in AUDIO_BUSES:
		var index := AudioServer.get_bus_index(pair[0])
		if index == -1:
			continue
		var volume: float = get_value("audio", pair[0], 0.8)
		AudioServer.set_bus_volume_db(index, linear_to_db(maxf(volume, 0.0001)))
		AudioServer.set_bus_mute(index, volume <= 0.001)


static func apply_video() -> void:
	var fullscreen: bool = get_value("video", "fullscreen", false)
	var mode := DisplayServer.window_get_mode()
	var is_fullscreen := mode == DisplayServer.WINDOW_MODE_FULLSCREEN or mode == DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN
	if fullscreen != is_fullscreen:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN if fullscreen else DisplayServer.WINDOW_MODE_WINDOWED)
	var vsync: bool = get_value("video", "vsync", true)
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_ENABLED if vsync else DisplayServer.VSYNC_DISABLED)
	Engine.max_fps = get_value("video", "max_fps", 0)
	apply_scale()


static func apply_scale() -> void:
	var tree := Engine.get_main_loop() as SceneTree
	if tree == null:
		return
	var value: float = get_value("video", "scale", 0.0)
	if value <= 0.0:
		var height := float(DisplayServer.window_get_size().y)
		value = clampf(floorf(height / 720.0 * 4.0) / 4.0, 1.0, 3.0)
	if not is_equal_approx(tree.root.content_scale_factor, value):
		tree.root.content_scale_factor = value


static func apply_controls() -> void:
	for pair in ACTIONS:
		var keycode: int = get_value("controls", pair[0], 0)
		if keycode != 0:
			_set_primary_key(pair[0], keycode)


static func primary_key_index(action: String) -> int:
	var events := InputMap.action_get_events(action)
	var found := -1
	for i in events.size():
		var key := events[i] as InputEventKey
		if key != null and key.physical_keycode != 0:
			found = i
	if found == -1:
		for i in events.size():
			if events[i] is InputEventKey:
				return i
	return found


static func key_name(action: String) -> String:
	var index := primary_key_index(action)
	if index == -1:
		return "—"
	var key := InputMap.action_get_events(action)[index] as InputEventKey
	return OS.get_keycode_string(key.physical_keycode if key.physical_keycode != 0 else key.keycode)


static func rebind(action: String, event: InputEventKey) -> void:
	var code := event.physical_keycode if event.physical_keycode != 0 else event.keycode
	_set_primary_key(action, code)
	config.set_value("controls", action, code)
	save()


static func _set_primary_key(action: String, physical_keycode: int) -> void:
	var replacement := InputEventKey.new()
	replacement.physical_keycode = physical_keycode as Key
	var index := primary_key_index(action)
	var events := InputMap.action_get_events(action)
	InputMap.action_erase_events(action)
	if index == -1:
		events.append(replacement)
	else:
		events[index] = replacement
	for event in events:
		InputMap.action_add_event(action, event)


static func reset_controls() -> void:
	for pair in ACTIONS:
		InputMap.action_erase_events(pair[0])
		for event in _defaults.get(pair[0], []):
			InputMap.action_add_event(pair[0], event)
	if config.has_section("controls"):
		config.erase_section("controls")
	save()


static func last_world() -> String:
	return get_value("game", "last_world", "")


static func set_last_world(scene_path: String) -> void:
	config.set_value("game", "last_world", scene_path)
	save()
