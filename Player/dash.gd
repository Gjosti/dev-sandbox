extends Node3D

@export var player: Player
@export var dash_cooldown: float = 0.5
@export var extra_dashes: int = 1
@export var dash_duration: float = 0.5
@export var dash_speed_modifier: float = 2.0
@export var min_dash_speed: float = 6
@export var max_dash_speed: float = 25
@onready var timer: Timer = $Timer

var direction: Vector3 = Vector3.ZERO
var dash_velocity: Vector3
var available_dashes: int = extra_dashes
var is_dashing: bool = false

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("dash"):
		dash()

func _physics_process(_delta: float) -> void:
	ground_refresh_dashes()

func dash() -> void:
	if available_dashes > 0 and timer.is_stopped():
		available_dashes -= 1
		is_dashing = true

		# Determine dash direction
		if player.direction.length() > 0.1:
			direction = player.direction.normalized()
		else:
			direction = -player.rig_pivot.global_transform.basis.z.normalized()

		var base_dash_speed: float = maxf(player._get_horizontal_velocity().length(), min_dash_speed)
		var clamped_dash_speed: float = clamp(base_dash_speed * dash_speed_modifier, min_dash_speed, max_dash_speed)
		dash_velocity = direction * clamped_dash_speed

		# Face dash direction
		if direction.length() > 0.01:
			var target_yaw: float = atan2(-direction.x, -direction.z)
			player.rig_pivot.rotation.y = target_yaw

		player.rig.travel("Dash")
		timer.start(dash_cooldown)

		# Maintain dash velocity for dash_duration
		var dash_timer: SceneTreeTimer = get_tree().create_timer(dash_duration)
		while not dash_timer.time_left == 0:
			player.velocity.x = dash_velocity.x
			player.velocity.z = dash_velocity.z
			await get_tree().process_frame

		is_dashing = false

func ground_refresh_dashes() -> void:
	if player.is_on_floor() and timer.is_stopped():
		available_dashes = extra_dashes
