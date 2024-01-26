extends Resource
class_name StorageDataClass

@export var data_items: Dictionary

func add_item(item: InventoryItem):
	if data_items.has(item.image):
		data_items[item.image] += item.amount
	else:
		data_items[item.image] = item.amount


func delete_item(item: InventoryItem):
	#var index: int = storage_data.find(item)
	#storage_data.pop_at(index)
	pass
