extends ActiveClass
class_name Building


func put_in_storage(items: Dictionary):
	for item in items:
		if items[item] != null:
			data.add_item(items[item])
