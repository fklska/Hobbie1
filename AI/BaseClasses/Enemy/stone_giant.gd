extends KinematicBodySelectedObejct
class_name StoneGiant

@export var data: BasicEnemyData

enum {
	IDLE,
	WALK,
	ATTACK,
	GET_HIT,
	DIE
}

var state = IDLE

func attack():
	pass
	
func get_hit():
	pass
