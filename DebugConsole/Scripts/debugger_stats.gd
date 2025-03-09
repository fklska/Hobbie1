extends PanelContainer


@onready var label: Label = $Label

func _ready():
	Engine.max_fps = 300

func _on_timer_timeout():
	var fps = Performance.get_monitor(Performance.TIME_FPS)
	var memory = Performance.get_monitor(Performance.MEMORY_STATIC)
	var nav = Performance.get_monitor(Performance.TIME_NAVIGATION_PROCESS)
	var ping = Performance.get_monitor(Performance.TIME_PROCESS)
	
	label.text = "FPS: {fps} \nMemory used: {mem} \nNAV Time TIME: {nav} \nCPU Time: {ping}".format(
		{
			"fps": str(fps),
			"mem": str(memory),
			"nav": str(nav),
			"ping": str(ping)
		}
	)
