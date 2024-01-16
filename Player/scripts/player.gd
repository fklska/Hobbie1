extends CharacterBody2D


const SPEED = 300.0
@onready var anim = $AnimatedSprite2D
# Get the gravity from the project settings to be synced with RigidBody nodes.


func _physics_process(delta):

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var direction = Input.get_axis("ui_left", "ui_right")
	var direction_y = Input.get_axis("ui_up", "ui_down")
		
	if direction or direction_y:
		velocity.y = direction_y * SPEED / 2
		velocity.x = direction * SPEED
		anim.play("run")
	else:
		velocity.y = move_toward(velocity.y, 0, SPEED)
		velocity.x = move_toward(velocity.x, 0, SPEED)
		anim.play("idle")
		
	if direction > 0:
		anim.flip_h = true
		
	if direction < 0:
		anim.flip_h = false

	move_and_slide()
