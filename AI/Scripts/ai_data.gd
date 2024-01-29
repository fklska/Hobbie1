extends Resource
class_name AIBackData

@export_category("Stats")
@export_range(1, 100) var HEALTH: int
@export_range(1, 100) var STRENCH: int
@export_range(1, 100) var AGILITY: int
@export_range(1, 100) var INTELECT: int
@export var name: String

@export_category("Items")
@export var weapon: WeaponClass
