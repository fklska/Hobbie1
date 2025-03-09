extends Node2D
class_name ThreadLearning

@export var chunk_size: int
var thread: Thread = Thread.new()

func _ready() -> void:
	thread.set_thread_safety_checks_enabled(false)
	thread.start(test)

func test():
	while true:
		call_deferred_thread_group("pixel2cell", get_global_mouse_position())

func pixel2cell(pixel:Vector2) -> Vector2i:
	var x = int(pixel.x/chunk_size)
	if sign(pixel.x) == -1:
		x -= 1

	var y = int(pixel.y/chunk_size)
	if sign(pixel.y) == 1:
		y += 1
	
	return Vector2i(x, y)
	
func _exit_tree():
	thread.wait_to_finish()
