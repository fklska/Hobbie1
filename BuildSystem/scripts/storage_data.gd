extends Resource
class_name StorageDataClass

@export var data_items: Array[InventoryItem]

func add_item(item: InventoryItem):
	var index = data_items.find(item)
	if index == -1:
		data_items.append(item)
	else:
		data_items[item].amount += item.amount


func delete_item(item: InventoryItem):
	#var index: int = storage_data.find(item)
	#storage_data.pop_at(index)
	pass
