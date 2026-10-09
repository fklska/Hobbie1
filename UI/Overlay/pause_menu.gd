extends Control

signal resume_requested
signal settings_requested
signal menu_requested
signal quit_requested

@onready var world_label: Label = %WorldName
@onready var resume_button: Button = %Resume


func _ready() -> void:
	hide()
	%Resume.pressed.connect(resume_requested.emit)
	%Settings.pressed.connect(settings_requested.emit)
	%ToMenu.pressed.connect(menu_requested.emit)
	%Quit.pressed.connect(quit_requested.emit)


func open(world_name: String) -> void:
	world_label.text = world_name
	world_label.visible = world_name != ""
	show()
	resume_button.grab_focus.call_deferred()
