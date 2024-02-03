extends Resource
class_name AIBackData

@export_category("Stats")
@export_range(1, 100) var HEALTH: int
@export_range(1, 100) var STRENCH: int
@export_range(1, 100) var AGILITY: int
@export_range(1, 100) var INTELECT: int
@export var name: String

@export_category("Items")
@export var WEAPON: WeaponClass
@export var Inventory: Array[InventoryItem]

@export var max_carry_weight: int = 10 :
	set(value):
		return STRENCH * 4 + value
	get:
		return STRENCH * 4 + max_carry_weight

func add_item(item: InventoryItem):
	var el: InventoryItem = has(item)
	if el == null:
		Inventory.append(item)
		return item

	el.increase_amount(item.amount)
	return el
		
func has(item: InventoryItem):
	for i: InventoryItem in Inventory:
		if i._compare(item.type):
			return i
	return null
	
func total_resourse_amount():
	var value = 0
	for i: InventoryItem in Inventory:
		value += i.amount
	return value
