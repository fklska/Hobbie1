extends CanvasLayer

const CLOSABLE_GROUP := &"closable_ui"

@onready var pause_menu: Control = $PauseMenu
@onready var settings: Control = $SettingsPanel
@onready var confirm_dialog: Control = $ConfirmDialog
@onready var loading: Control = $LoadingScreen
@onready var fps_label: Label = $Fps


func _ready() -> void:
	GameSettings.load_and_apply()
	get_tree().root.size_changed.connect(GameSettings.apply_scale)
	pause_menu.resume_requested.connect(resume)
	pause_menu.settings_requested.connect(open_settings)
	pause_menu.menu_requested.connect(_ask_to_menu)
	pause_menu.quit_requested.connect(_ask_quit)
	settings.closed.connect(_on_settings_closed)


func _process(_delta: float) -> void:
	fps_label.visible = GameSettings.get_value("video", "show_fps", false)
	if fps_label.visible:
		fps_label.text = "FPS %d" % Engine.get_frames_per_second()


func in_game() -> bool:
	return is_instance_valid(Game.World) and not Game.Ended


func is_busy() -> bool:
	return loading.visible or confirm_dialog.visible or settings.visible or pause_menu.visible


func _input(event: InputEvent) -> void:
	if not event.is_action_pressed("ESC"):
		return
	if loading.visible:
		pass
	elif confirm_dialog.visible:
		confirm_dialog.close()
	elif settings.visible:
		settings.close()
	elif pause_menu.visible:
		resume()
	elif in_game():
		if not close_open_windows():
			pause()
	else:
		return
	get_viewport().set_input_as_handled()


func close_open_windows() -> bool:
	var closed := false
	for node in get_tree().get_nodes_in_group(CLOSABLE_GROUP):
		var control := node as CanvasItem
		if control != null and control.visible:
			control.hide()
			closed = true
	return closed


func pause() -> void:
	get_tree().paused = true
	var world_name := ""
	if is_instance_valid(Game.World):
		world_name = Game.World.WorldName
	pause_menu.open(world_name)


func resume() -> void:
	pause_menu.hide()
	if not Game.Ended:
		get_tree().paused = false


func open_settings() -> void:
	settings.open()


func _on_settings_closed() -> void:
	if pause_menu.visible:
		pause_menu.resume_button.grab_focus()


func confirm(title: String, body: String, ok_text: String, callback: Callable, danger := true) -> void:
	confirm_dialog.open(title, body, ok_text, callback, danger)


func _ask_to_menu() -> void:
	confirm("Выйти в главное меню?", "Текущая партия будет прервана.", "Выйти", func() -> void:
		pause_menu.hide()
		Game.ToMenu())


func _ask_quit() -> void:
	confirm("Выйти из игры?", "", "Выйти", get_tree().quit)


func start_world(scene_path: String, spawn: Vector2, continue_run := false) -> void:
	loading.open("Загрузка мира", "Читаем сохранение")
	await get_tree().process_frame
	if ResourceLoader.load_threaded_request(scene_path, "", true) != OK:
		loading.close()
		return
	var progress := []
	var status := ResourceLoader.load_threaded_get_status(scene_path, progress)
	while status == ResourceLoader.THREAD_LOAD_IN_PROGRESS:
		loading.set_ratio(progress[0] * 0.8)
		await get_tree().process_frame
		status = ResourceLoader.load_threaded_get_status(scene_path, progress)
	if status != ResourceLoader.THREAD_LOAD_LOADED:
		loading.close()
		confirm("Мир не загрузился", "Файл мира повреждён или удалён.", "Понятно", Callable(), false)
		return
	var scene := ResourceLoader.load_threaded_get(scene_path)
	loading.set_ratio(0.9, "Строим мир")
	await get_tree().process_frame
	await get_tree().process_frame
	GameSettings.set_last_world(scene_path)
	if continue_run and Game.has_method("ContinueRun"):
		Game.ContinueRun(scene_path)
	else:
		Game.StartWorld(scene_path, spawn)
	scene = null
	await get_tree().process_frame
	loading.close()
