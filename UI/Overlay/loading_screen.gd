extends Control

const TIPS := [
	"Зажми ЛКМ на дереве или камне рядом с героем, чтобы добыть ресурс.",
	"F или ПКМ: удар по врагу в сторону курсора.",
	"B открывает меню строительства, Tab открывает инвентарь.",
	"Z и X меняют масштаб камеры.",
	"Ночью опаснее: держи героя ближе к деревне.",
	"Жители сами носят добычу в ратушу.",
	"Esc ставит игру на паузу.",
]

@onready var title: Label = %Title
@onready var stage: Label = %Stage
@onready var bar: ProgressBar = %Bar
@onready var tip: Label = %Tip

var stage_text := ""
var elapsed := 0.0
var tip_index := 0


func _ready() -> void:
	hide()
	set_process(false)


func open(title_text: String, stage_value := "") -> void:
	title.text = title_text
	bar.value = 0
	elapsed = 0.0
	tip_index = randi() % TIPS.size()
	tip.text = TIPS[tip_index]
	set_stage(stage_value)
	show()
	set_process(true)


func set_stage(value: String) -> void:
	stage_text = value
	stage.text = value


func set_ratio(ratio: float, value := "") -> void:
	bar.max_value = 1.0
	bar.value = clampf(ratio, 0.0, 1.0)
	if value != "":
		set_stage(value)


func close() -> void:
	hide()
	set_process(false)


func _process(delta: float) -> void:
	elapsed += delta
	stage.text = stage_text + ".".repeat(int(elapsed * 2.0) % 4)
	if int(elapsed / 6.0) != int((elapsed - delta) / 6.0):
		tip_index = (tip_index + 1) % TIPS.size()
		tip.text = TIPS[tip_index]
