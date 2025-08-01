extends Node
class_name GDUtils

static func getLocalCell(coords: Vector2):
	var nx = int(coords.x) % (64 * 8)
	var ny = int(coords.y) % (64 * 8)
	return Vector2i(nx, ny)

static func getChunkCell(coords: Vector2):
	return coords / (64 * 8)
