@tool
extends  Node2D

func _ready() -> void:
	var point_list : Array[Vector2] = [Vector2(16,1), Vector2(3,15), Vector2(32,3), Vector2(2,54), Vector2(42,14)]
	var voronoi : VoronoiSweepline = VoronoiSweepline.new()
	voronoi.generate(point_list, [0, 64, 0, 64 ])
	voronoi.relax()
	display(voronoi.cells)

func display(cells : Dictionary) -> void:
	if get_child_count() > 1:
		get_child(1).free()
	
	var root : Node2D = Node2D.new()
	root.position = Vector2(80,80)	# Shift the diagram so it clear the buttons
	add_child(root)
	# Display the cells
	for cell_name : Vector2 in cells:
		var poly = Polygon2D.new()
		poly.polygon = PackedVector2Array( cells[cell_name][0] )	# cell[0] is the cell's polygon. cell[1] are the neigboring cells
		poly.color = Color(randf_range(0, 1),randf_range(0, 1),randf_range(0, 1))
		root.add_child(poly)
