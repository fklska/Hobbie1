extends PanelContainer

@export var root_container: GridContainer
@export var slot_node: PackedScene
@export var data: AllInventoryObjects

func _ready():
	pass


func _input(event: InputEvent):
	if event.is_action_pressed("action"):
		visible = !visible
