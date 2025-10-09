extends Node3D

@export var player: Player
@export var jump_height: float = 4
@export var jump_time_to_peak: float = 0.5
@export var jump_time_to_descent: float = 0.25
@export var extra_jumps: int = 1

@onready var jump_velocity: float = (2.0 * jump_height) / jump_time_to_peak
@onready var jump_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_peak)
@onready var fall_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_descent)
@onready var jumps_left: int = extra_jumps

var coyote_time: float = 0.1
var coyote_timer: float = 0.0

func _physics_process(delta: float) -> void:
	if player.is_on_floor():
		jumps_left = extra_jumps
	handle_jump_input()
		# Coyote time 
	if player.is_on_floor():
		coyote_timer = coyote_time
	else:
		coyote_timer = max(0.0, coyote_timer - delta)

func handle_jump_input() -> void:
	if Input.is_action_just_pressed("jump"):
		if player.is_on_floor() or coyote_timer > 0.0:
			player.velocity.y += jump_velocity
			coyote_timer = 0.0
			player.rig.travel("Jump")
		elif jumps_left > 0:
			jumps_left -= 1
			player.velocity.y = jump_velocity
			player.rig.travel("Jump")

# TODO this should probably be handled in player.gd
func get_gravity(current_velocity_y: float) -> float:
	return jump_gravity if current_velocity_y > 0.0 else fall_gravity
