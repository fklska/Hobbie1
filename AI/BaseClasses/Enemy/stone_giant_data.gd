extends Resource
class_name BasicEnemyData

@export_category("Stats")
@export_range(1, 100) var HEALTH: int
@export_range(1, 100) var STRENCH: int
@export_range(1, 100) var AGILITY: int
@export_range(1, 100) var INTELECT: int

@export var NAME: String
@export_multiline var DESCRIPTION: String
