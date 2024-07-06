extends PanelContainer
class_name MouseInfoPanel

static var Marker: bool = false

static func setPosSignal():
	Marker = true
	
func _process(delta):
	if Marker:
		global_position = get_global_mouse_position()
		Marker = false
	
