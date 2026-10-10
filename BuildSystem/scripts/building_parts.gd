extends Resource
class_name BuildingParts

@export var ground: Texture2D
@export var parts: Array[Texture2D] = []
@export var positions := PackedVector2Array()
@export var sort_rows := PackedFloat32Array()
@export var preview: Texture2D
@export var turns: Array[Resource] = []


func turned(turn: int) -> BuildingParts:
	if turn <= 0 or turn > turns.size():
		return self
	return turns[turn - 1] as BuildingParts
