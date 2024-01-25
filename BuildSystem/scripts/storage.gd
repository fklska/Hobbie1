extends Resource
class_name StorageDataClass

@export var storage_data: Array[InventoryItem]

func add_item(item: InventoryItem):
	storage_data.append(item)
	
func delete_item(item: InventoryItem):
	var index: int = storage_data.find(item)
	storage_data.pop_at(index)
