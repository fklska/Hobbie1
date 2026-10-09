extends Resource
class_name BuildingParts

@export var ground: Texture2D
@export var parts: Array[Texture2D] = []
@export var positions := PackedVector2Array()
@export var sort_rows := PackedFloat32Array()
