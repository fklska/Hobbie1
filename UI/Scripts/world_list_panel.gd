extends Control

signal back_requested
signal create_requested

const ITEM := preload("res://UI/prefabs/world_list_item.tscn")

@onready var list: VBoxContainer = %List
@onready var scroll: ScrollContainer = %Scroll
@onready var empty: Control = %Empty
@onready var count: Label = %Count


func _ready() -> void:
	%Back.pressed.connect(back_requested.emit)
	%Create.pressed.connect(create_requested.emit)


func open() -> void:
	show()
	refresh()


func refresh() -> void:
	for child in list.get_children():
		child.queue_free()
	var worlds := WorldStore.list_worlds()
	for world in worlds:
		var item: WorldListItem = ITEM.instantiate()
		list.add_child(item)
		item.setup(world)
		item.play_requested.connect(_on_play_requested)
		item.delete_requested.connect(_ask_delete)
	scroll.visible = not worlds.is_empty()
	empty.visible = worlds.is_empty()
	count.text = "Миров: %d" % worlds.size()
	if worlds.is_empty():
		%Create.grab_focus.call_deferred()
	else:
		%Back.grab_focus.call_deferred()


func _on_play_requested(world: SimpleGeneratorData, fresh: bool) -> void:
	if not fresh or not WorldStore.has_run(world):
		WorldStore.play(world, fresh)
		return
	var day := WorldStore.run_day(world)
	var body := "Сохранённая партия (день %d из 30) пропадёт." % day if day > 0 else "Сохранённая партия пропадёт."
	Overlay.confirm("Начать партию заново?", body, "Начать", func() -> void:
		WorldStore.delete_run(world)
		WorldStore.play(world, true))


func _ask_delete(world: SimpleGeneratorData) -> void:
	Overlay.confirm("Удалить мир «%s»?" % world.WorldName, "Мир и его сохранение пропадут насовсем.", "Удалить", func() -> void:
		WorldStore.delete(world)
		refresh())
