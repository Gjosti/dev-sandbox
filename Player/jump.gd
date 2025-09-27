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


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

	if player.is_on_floor():
		jumps_left = extra_jumps


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta: float) -> void:
	handle_jump_input()

func handle_jump_input() -> void:
	if Input.is_action_just_pressed("jump"):
		if player.is_on_floor() or player.coyote_timer > 0.0:
			player.velocity.y = jump_velocity
			player.coyote_timer = 0.0
			player.rig.travel("Jump")
		elif player.jumps_left > 0:
			player.jumps_left -= 1
			player.velocity.y = jump_velocity
			player.rig.travel("Jump")

func get_gravity(current_velocity_y: float) -> float:
	return jump_gravity if current_velocity_y > 0.0 else fall_gravity
