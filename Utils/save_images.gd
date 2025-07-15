@tool
extends Node2D

func _ready() -> void:
	var res = load("res://SavedWorlds/Eternal Dominion of the Ancients.tres")
	res.HeightMap.save_png("res://Utils/heightMap.png")
	res.HeatMap.save_png("res://Utils/heatMap.png")
	res.MoistureMap.save_png("res://Utils/moistureMap.png")
	res.BiomeMap.save_png("res://Utils/biomeMap.png")
