extends StaticBody2D
class_name Building

@export var data: StorageDataClass

func put_in_storage(items: Dictionary):
	for item in items:
		if items[item] != null:
			data.add_item(items[item])
