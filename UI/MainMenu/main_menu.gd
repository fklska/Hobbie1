extends Control
class_name MainMenu

@onready var main: Control = %Main
@onready var continue_button: Button = %Continue
@onready var continue_hint: Label = %ContinueHint
@onready var generation_panel: Control = $GenerationPanel
@onready var world_list: Control = $WorldListPanel


func _ready() -> void:
	%NewWorld.pressed.connect(open_panel.bind(generation_panel))
	%LoadWorld.pressed.connect(open_panel.bind(world_list))
	%Settings.pressed.connect(func() -> void: Overlay.open_settings())
	%Quit.pressed.connect(func() -> void: get_tree().quit())
	continue_button.pressed.connect(_on_continue)
	generation_panel.back_requested.connect(show_main)
	world_list.back_requested.connect(show_main)
	world_list.create_requested.connect(open_panel.bind(generation_panel))
	visibility_changed.connect(_on_visibility_changed)
	show_main()


func _on_visibility_changed() -> void:
	if visible:
		show_main()


func show_main() -> void:
	generation_panel.hide()
	world_list.hide()
	main.show()
	refresh_continue()
	(continue_button if continue_button.visible else %NewWorld).grab_focus.call_deferred()


func open_panel(panel: Control) -> void:
	main.hide()
	generation_panel.hide()
	world_list.hide()
	panel.call("open")


func refresh_continue() -> void:
	var world := WorldStore.find(GameSettings.last_world())
	if world == null:
		var worlds := WorldStore.list_worlds()
		world = worlds[0] if not worlds.is_empty() else null
	continue_button.visible = world != null
	continue_hint.visible = world != null
	if world == null:
		return
	continue_button.set_meta("world", world)
	var day := WorldStore.run_day(world)
	continue_hint.text = world.WorldName + ("" if day <= 0 else ", день %d" % day)


func _on_continue() -> void:
	var world: SimpleGeneratorData = continue_button.get_meta("world")
	WorldStore.play(world)


func _unhandled_input(event: InputEvent) -> void:
	if not visible or not event.is_action_pressed("ESC"):
		return
	if generation_panel.visible and not generation_panel.call("is_busy"):
		show_main()
	elif world_list.visible:
		show_main()
	else:
		return
	get_viewport().set_input_as_handled()
