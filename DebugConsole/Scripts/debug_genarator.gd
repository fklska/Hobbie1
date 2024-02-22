extends PanelContainer

var generator: MapGenerator
@onready var slider: HSlider = $VBoxContainer/HSlider
func _ready():
	generator = get_parent().get_parent().get_parent().get_parent()


func _on_generate_button_pressed():
	if generator is MapGenerator:
		generator.generate()
	else:
		print_debug("Cant get main node")


func _on_clear_button_pressed():
	if generator is MapGenerator:
		generator.clear()
	else:
		print_debug("Cant get main node")


func _on_h_slider_drag_ended(value_changed):
	if value_changed:
		generator.SIZE = Vector2(slider.value, slider.value)
