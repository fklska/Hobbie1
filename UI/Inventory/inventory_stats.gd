extends VBoxContainer

const ICONS := "res://UI/Icons/%s.png"
const STOCK := [
	["wood", "Дерево"],
	["stone", "Камень"],
	["iron", "Железо"],
	["gold", "Золото"],
	["food", "Еда"],
	["coins", "Монеты"],
]

@onready var hero_list: VBoxContainer = %Hero
@onready var stock_list: VBoxContainer = %Stock

var values := {}
var timer := 0.0


func _ready() -> void:
	_add_row(hero_list, "health", "heart", "Здоровье")
	_add_row(hero_list, "damage", "sword", "Урон")
	if Game.get("ToolSpeed") != null:
		_add_row(hero_list, "tools", "tool_pickaxe", "Скорость добычи")
	for entry in STOCK:
		if _has_kind(entry[0]):
			_add_row(stock_list, entry[0], entry[0], entry[1])
	visibility_changed.connect(refresh)
	Game.StockChanged.connect(refresh)
	refresh()


func _process(delta: float) -> void:
	timer -= delta
	if timer <= 0.0:
		timer = 0.25
		refresh()


func refresh() -> void:
	if not is_visible_in_tree():
		return
	var player = Game.Player
	if is_instance_valid(player):
		values.health.text = "%d / %d" % [ceili(player.Health), player.MaxHealth]
	values.damage.text = str(_damage(player))
	if values.has("tools"):
		values.tools.text = "×%s" % snappedf(Game.get("ToolSpeed"), 0.01)
	var capped: bool = Game.has_method("Capacity")
	for entry in STOCK:
		var kind: String = entry[0]
		if not values.has(kind):
			continue
		var amount: int = Game.GetStock(kind)
		values[kind].text = "%d / %d" % [amount, Game.Capacity(kind)] if capped and kind != "coins" else str(amount)


func _damage(player) -> int:
	var damage = Game.get("HeroDamage")
	if damage != null:
		return damage
	if not is_instance_valid(player):
		return 0
	return player.AttackDamage * (3 if Game.get("SwordForged") else 1)


func _has_kind(kind: String) -> bool:
	match kind:
		"food":
			return Game.has_method("Capacity")
		"coins":
			return Game.has_method("Capacity") or Game.has_method("HasRun")
	return true


func _add_row(list: Container, key: String, icon: String, title: String) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)
	var picture := TextureRect.new()
	picture.texture = load(ICONS % icon)
	picture.custom_minimum_size = Vector2(28, 28)
	picture.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	picture.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	var name_label := Label.new()
	name_label.text = title
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var value := Label.new()
	value.theme_type_variation = &"ValueLabel"
	value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	row.add_child(picture)
	row.add_child(name_label)
	row.add_child(value)
	list.add_child(row)
	values[key] = value
