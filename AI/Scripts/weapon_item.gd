extends InventoryItem
class_name WeaponClass

@export_category("Weapon")
@export var damage: int
@export var required_strench: int

func _init(_texture: Texture2D, _name: String, _amount: int, _type: String, _damage: int, _required_strench: int):
	damage = _damage
	required_strench = _required_strench
	super._init(_texture, _name, _amount, _type)
	
