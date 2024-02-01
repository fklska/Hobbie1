extends PanelContainer


@onready var label: Label = $Label


func _on_timer_timeout():
	var fps = Performance.get_monitor(Performance.TIME_FPS)
	var memory = Performance.get_monitor(Performance.MEMORY_STATIC)
	var objects = Performance.get_monitor(Performance.OBJECT_NODE_COUNT)
	var ping = Performance.get_monitor(Performance.TIME_PROCESS)
	
	label.text = "FPS: {fps} \nMemory used: {mem} \nObjects: {obj} \nRenderPing: {ping}".format(
		{
			"fps": str(fps),
			"mem": str(memory),
			"obj": str(objects),
			"ping": str(ping)
		}
	)
