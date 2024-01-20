extends CanvasLayer
class_name UI

@onready var player: Player = $"../Player"
@onready var portret: TextureRect = $MarginContainer/Panel/SelectorInfo/Portret
func _ready():
	pass

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	var sprite: AnimatedSprite2D = player.get_child(1)
	var frames: SpriteFrames = sprite.sprite_frames
	portret.texture = frames.get_frame_texture(sprite.animation, sprite.frame)

@onready var strench: Label = $MarginContainer/Panel/SelectorInfo/STR
@onready var agility: Label = $MarginContainer/Panel/SelectorInfo/AGL
@onready var intelect: Label = $MarginContainer/Panel/SelectorInfo/INT

@onready var inv = $MarginContainer/Panel/MarginContainer/GridContainer
var slot = preload("res://UI/slot.tscn")
func update_ui():
	clear_ui()
	strench.text = str(player.STRENCH)
	agility.text = str(player.AGILITY)
	intelect.text = str(player.INTELECT)
	
	for obj: Texture2D in player.Inventory:
		var prefab: TextureRect = slot.instantiate()
		prefab.get_child(0).texture = obj
		prefab.get_child(1).text = str(player.Inventory[obj])
		inv.add_child(prefab)


func clear_ui():
	var objects = inv.get_children()
	for obj in objects:
		obj.queue_free()
