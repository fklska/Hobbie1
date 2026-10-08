extends PanelContainer

const LOW_HEALTH := 0.3

@onready var bar: Range = %HPbar
@onready var value_label: Label = %Value
@onready var heart: TextureRect = %Heart

var pulse: Tween


func _ready() -> void:
	bar.value_changed.connect(_refresh.unbind(1))
	bar.changed.connect(_refresh)
	_refresh()


func _refresh() -> void:
	value_label.text = "%d / %d" % [ceili(bar.value), int(bar.max_value)]
	var low := bar.value <= bar.max_value * LOW_HEALTH
	if low and pulse == null:
		pulse = create_tween().set_loops()
		pulse.tween_property(heart, "modulate", Color(1, 0.4, 0.4), 0.35)
		pulse.tween_property(heart, "modulate", Color.WHITE, 0.35)
	elif not low and pulse != null:
		pulse.kill()
		pulse = null
		heart.modulate = Color.WHITE
