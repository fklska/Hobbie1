extends BaseDataClass
class_name StorageDataClass


func add_item(item: InventoryItem):
	var index = data_items.find(item)
	var value = 0

	if index == -1:
		data_items.append(item)
	else:
		value = data_items[index].amount + item.amount
		data_items[index].amount = value
	
func delete_item(item: InventoryItem):
	#var index: int = storage_data.find(item)
	#storage_data.pop_at(index)
	pass
