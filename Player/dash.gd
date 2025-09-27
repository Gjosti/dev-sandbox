extends Node3D

@export var player: Player
@export var dash_cooldown: float = 0.5
@export var dash_duration: float = 0.3
@export var dash_speed_modifier: float = 2.0
@export var time_remaining: float = 0.0
@onready var timer: Timer = $Timer

var direction: Vector3 = Vector3.ZERO
var previous_velocity: Vector3 = Vector3.ZERO
var dash_velocity: Vector3

func _physics_process(delta: float) -> void:
	dash_velocity = direction * player.speed * dash_speed_modifier
	player.velocity.x = dash_velocity.x
	player.velocity.z = dash_velocity.z

	# Face dash direction while dashing
	if time_remaining > 0 and direction.length() > 0.01:
		var target_yaw = atan2(-direction.x, -direction.z)
		player.rig_pivot.rotation.y = target_yaw

	time_remaining -= delta
	if time_remaining <= 0 and player.is_on_floor():
		direction = Vector3.ZERO

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("dash") and timer.is_stopped():
		if player.direction.length() > 0.1:
			direction = player.direction.normalized()
		else:
			direction = - player.rig_pivot.global_transform.basis.z.normalized()

		print("Dashable")
		player.rig.travel("Dash")
		timer.start(dash_cooldown)
		time_remaining = dash_duration
