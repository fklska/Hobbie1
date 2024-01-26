extends ActiveClass
class_name Building


func action(items: Dictionary):
	for item in items:
		if items[item] != null:
			data.data.append(items[item])

	for i in data.data:
		print_debug(i)
	print_debug(data)
