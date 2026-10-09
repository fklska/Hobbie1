extends VBoxContainer

const ICONS := "res://UI/Icons/%s.png"

@onready var hero_list: VBoxContainer = %Hero

var values := {}
var icons := {}
var timer := 0.0


func _ready() -> void:
	_add_row("health", load(ICONS % "heart"), "Здоровье")
	_add_row("weapon", null, "Оружие")
	_add_row("damage", load(ICONS % "sword"), "Урон")
	_add_row("tools", load(ICONS % "tool_pickaxe"), "Скорость добычи")
	visibility_changed.connect(refresh)
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
	var weapon: Dictionary = Game.GetWeaponStats(Game.HeroWeapon)
	values.weapon.text = weapon.title
	icons.weapon.texture = load(weapon.icon)
	values.damage.text = str(Game.HeroDamage)
	values.tools.text = "×%s" % snappedf(Game.ToolSpeed, 0.1)


func _add_row(key: String, texture: Texture2D, title: String) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)
	var picture := TextureRect.new()
	picture.texture = texture
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
	hero_list.add_child(row)
	values[key] = value
	icons[key] = picture
