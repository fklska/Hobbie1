extends CharacterBody2D


const SPEED = 500.0
@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var animPlayer = $AnimationPlayer
# Get the gravity from the project settings to be synced with RigidBody nodes.

enum {
	RUN,
	ATTACK,
}
var state = RUN

func _ready():
	var sprite = $Minimap

func _physics_process(delta):
	match state:
		RUN:
			run()
		ATTACK:
			attack()
	
	move_and_slide()	

func run():
	var direction = Input.get_axis("ui_left", "ui_right")
	var direction_y = Input.get_axis("ui_up", "ui_down")
	
	if direction or direction_y and state == RUN:
		velocity.y = direction_y * SPEED / 2
		velocity.x = direction * SPEED
		animPlayer.play("run")
		
		if direction > 0:
			anim.scale.x = -1
		
		if direction < 0:
			anim.scale.x = 1
	else:
		velocity.y = move_toward(velocity.y, 0, SPEED)
		velocity.x = move_toward(velocity.x, 0, SPEED)
		animPlayer.play("idle")
		
	if Input.is_action_just_pressed("attack"):
		state = ATTACK
		
func attack():
	animPlayer.play("attack")
	await animPlayer.animation_finished
	state = RUN


func clearing():
	for obj in targets:
		obj.get_damage()


var targets = []
func _on_area_2d_body_entered(body: StaticBody2D):
	targets.push_back(body)


func _on_area_2d_body_exited(body):
	targets.pop_back()
