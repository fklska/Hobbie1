class_name StockBar
extends HBoxContainer

const RESOURCES := [
	["wood", "Дерево"],
	["stone", "Камень"],
	["iron", "Железо"],
	["gold", "Золото"],
	["food", "Еда"],
	["coins", "Монеты"],
]

var values := {}


static func available() -> Array:
	return RESOURCES.filter(func(entry: Array) -> bool: return has_kind(entry[0]))


static func has_kind(kind: String) -> bool:
	match kind:
		"food":
			return Game.has_method("Capacity")
		"coins":
			return Game.has_method("Capacity") or Game.has_method("HasRun")
	return true


static func icon(kind: String) -> Texture2D:
	return load("res://UI/Icons/%s.png" % kind)


static func amount_text(kind: String, separator := "/") -> String:
	var amount: int = Game.GetStock(kind)
	if kind != "coins" and Game.has_method("Capacity"):
		return "%d%s%d" % [amount, separator, Game.Capacity(kind)]
	return str(amount)


func _ready() -> void:
	for entry in available():
		var item := HBoxContainer.new()
		item.add_theme_constant_override("separation", 6)
		item.tooltip_text = entry[1]
		item.mouse_filter = Control.MOUSE_FILTER_PASS
		var picture := TextureRect.new()
		picture.texture = icon(entry[0])
		picture.custom_minimum_size = Vector2(24, 24)
		picture.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		picture.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		picture.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		var label := Label.new()
		label.theme_type_variation = &"HudLabel"
		item.add_child(picture)
		item.add_child(label)
		add_child(item)
		values[entry[0]] = label
	Game.StockChanged.connect(refresh)
	Game.ProgressChanged.connect(refresh)
	refresh()


func refresh() -> void:
	for kind in values:
		values[kind].text = amount_text(kind)
