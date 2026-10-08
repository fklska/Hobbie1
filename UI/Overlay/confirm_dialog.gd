extends Control

@onready var title: Label = %Title
@onready var text: Label = %Text
@onready var ok_button: Button = %Ok
@onready var cancel_button: Button = %Cancel

var on_ok: Callable


func _ready() -> void:
	hide()
	ok_button.pressed.connect(_on_ok)
	cancel_button.pressed.connect(close)


func open(title_text: String, body: String, ok_text: String, callback: Callable, danger := true) -> void:
	title.text = title_text
	text.text = body
	text.visible = body != ""
	ok_button.text = ok_text
	ok_button.theme_type_variation = &"DangerButton" if danger else &"PrimaryButton"
	on_ok = callback
	show()
	cancel_button.grab_focus.call_deferred()


func close() -> void:
	hide()
	on_ok = Callable()


func _on_ok() -> void:
	var callback := on_ok
	close()
	if callback.is_valid():
		callback.call()
