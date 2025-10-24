extends Node3D

@export_group("Jump Settings")
@export var player: Player
@export var jump_height: float = 4
@export var jump_time_to_peak: float = 0.5
@export var jump_time_to_descent: float = 0.25
@export var extra_jumps: int = 1

@export_group("Crouch/High Jump")
@export var crouch_jump_enabled: bool = true
@export var crouch_jump_height: float = 8
@export var crouch_jump_time_to_peak: float = 0.5
@export var crouch_jump_time_to_descent: float = 0.25

@export_group("Speed Jump")
@export var speed_jump_enabled: bool = true #(TODO) Jumping from the ground while above a certain horizontal velocity threshold adds a slight velocity boost

# Regular Jump
@onready var jump_velocity: float = (2.0 * jump_height) / jump_time_to_peak
@onready var jump_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_peak)
@onready var fall_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_descent)

# Crouch/High Jump
@onready var crouch_jump_velocity: float = (2.0 * crouch_jump_height) / crouch_jump_time_to_peak

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

	#Cancel jump early
	if Input.is_action_just_released("jump") and player.velocity.y >= 0:
		player.velocity.y *= 0.4

func handle_jump_input() -> void:
	if Input.is_action_just_pressed("jump"): 
		if crouch_jump_enabled and player.rig.is_crouching():
			crouch_jump()
		else: 
			jump()


func jump() -> void:
	if player.is_on_floor() or coyote_timer > 0.0:
		player.velocity.y += jump_velocity
		coyote_timer = 0.0
		player.rig.travel("Jump")
	elif jumps_left > 0:
		jumps_left -= 1
		player.velocity.y = jump_velocity
		player.rig.travel("Jump")


func crouch_jump() -> void:
	if player.is_on_floor() or coyote_timer > 0.0:
		player.velocity.y += crouch_jump_velocity
		coyote_timer = 0.0
		player.rig.travel("CrouchJump")
	elif jumps_left > 0:
		jumps_left -= 1
		player.velocity.y = jump_velocity #So that the player cannot high jump in air.
		player.rig.travel("Jump")

# TODO Recheck if needed
func get_gravity(current_velocity_y: float) -> float:
	return jump_gravity if current_velocity_y > 0.0 else fall_gravity
