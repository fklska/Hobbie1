extends Node2D

@onready var main_basic_villager: BaseVillager = $MainBasicVillager

func _ready() -> void:
	Navigation.bake_navigation_on_agent(main_basic_villager)
