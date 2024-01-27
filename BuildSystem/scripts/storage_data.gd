extends Resource
class_name StorageDataClass

@export var data_items: Dictionary

func put_in_storage(item: InventoryItem):
	data_items[item.image] += item.amount


func get_from_storage(item: InventoryItem):
	data_items[item.image] -= item.amount
