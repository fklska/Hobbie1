extends InventoryItem
class_name WeaponClass

@export_category("Weapon")
@export var damage: int
@export var required_strench: int

@export var shape: CapsuleShape2D = CapsuleShape2D.new()

func _init(_texture: Texture2D, _name: String, _amount: int, _type: int = 0, _description = "Description", _damage: int = 10, _required_strench: int = 10):
	damage = _damage
	required_strench = _required_strench
	super._init(_texture, _name, _amount, _type, _description)
	set_collider_shape()
	type = Globals.InventoryItemTypes.WEAPON
	

func set_collider_shape():
	shape.height = 35
	shape.radius = 5
	
