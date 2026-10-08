extends Node2D
class_name Arrow

enum Style { ARROW, STONE, BULLET }

@export var speed := 520.0
@export var style := Style.ARROW

var damage := 0
var group := &"village"
var max_distance := 600.0
var source: Node
var _direction := Vector2.RIGHT
var _travelled := 0.0


func setup(direction: Vector2, amount: int, hits: StringName, shooter: Node, reach: float, projectile_speed := 0.0):
	_direction = direction.normalized()
	rotation = _direction.angle()
	damage = amount
	group = hits
	source = shooter
	max_distance = reach
	if projectile_speed > 0.0:
		speed = projectile_speed


func _ready():
	if has_node(^"Sprite"):
		$Sprite.visible = style == Style.ARROW


func _physics_process(delta: float):
	var step := speed * delta
	position += _direction * step
	_travelled += step
	for node: Node2D in get_tree().get_nodes_in_group(group):
		if node.is_in_group("enemies") and node.get("dead"):
			continue
		if global_position.distance_to(Mob.target_point(node)) <= Mob.target_radius(node) + 4.0:
			Game.Damage(node, damage)
			if is_instance_valid(source) and source.has_method("on_projectile_hit"):
				source.on_projectile_hit(damage)
			queue_free()
			return
	if _travelled >= max_distance:
		queue_free()


func _draw():
	if style == Style.ARROW and has_node(^"Sprite"):
		return
	match style:
		Style.STONE:
			draw_circle(Vector2.ZERO, 3.0, Color(0.45, 0.43, 0.4))
		Style.BULLET:
			draw_circle(Vector2.ZERO, 2.0, Color(0.15, 0.15, 0.15))
		_:
			draw_line(Vector2(-12, 0), Vector2(6, 0), Color(0.5, 0.36, 0.2), 2.0)
			draw_colored_polygon(PackedVector2Array([Vector2(10, 0), Vector2(5, -3), Vector2(5, 3)]), Color(0.75, 0.75, 0.8))
			draw_line(Vector2(-12, 0), Vector2(-15, -3), Color(0.9, 0.9, 0.85), 1.5)
			draw_line(Vector2(-12, 0), Vector2(-15, 3), Color(0.9, 0.9, 0.85), 1.5)
