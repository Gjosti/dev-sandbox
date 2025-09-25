extends CharacterBody3D
class_name Player

# Movement Settings
@export_group("Movement Settings")
@export var jump_height: float = 4
@export var jump_time_to_peak: float = 0.5
@export var jump_time_to_descent: float = 0.25
@export var extra_jumps: int = 1
@export var speed: float = 6.0
@export_range (0, 30, 0.5) var air_control_lerp: float = 10 # Controls air movement responsiveness

# Camera Settings
@export_group("Camera Settings")
@export var mouse_sensitivity: float = 0.00075
@export var camera_scroll_sensitivity: float = 0.25
@export var min_camera_rotation: float = -90
@export var max_camera_rotation: float = 90
@export var min_camera_distance: float = 1
@export var max_camera_distance: float = 15

# State Variables
var _look := Vector2.ZERO
var jumps_left: int = extra_jumps
var previous_velocity: Vector3
var coyote_time: float = 0.1
var coyote_timer: float = 0.0

# Node References
@onready var horizontal_pivot: Node3D = $HorizontalPivot
@onready var vertical_pivot: Node3D = $HorizontalPivot/VerticalPivot
@onready var player_mesh: Node3D = $MeshInstance3D
@onready var collision_shape_3d: CollisionShape3D = $CollisionShape3D
@onready var rig_pivot: Node3D = $RigPivot
@onready var camera_arm: SpringArm3D = $HorizontalPivot/VerticalPivot/CameraArm

# Calculated Jump Variables
@onready var jump_velocity: float = (2.0 * jump_height) / jump_time_to_peak
@onready var jump_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_peak)
@onready var fall_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_descent)

# Initialization
func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

# Physics Processing
func _physics_process(delta: float) -> void:
	frame_camera_rotation()
	apply_gravity(delta)
	handle_movement(delta)
	handle_air_control(delta)
	# handle_dash_input()
	handle_jump_input()

	move_and_slide()
	previous_velocity = velocity

	 # Coyote time 
	if is_on_floor():
		coyote_timer = coyote_time
	else:
		coyote_timer = max(0.0, coyote_timer - delta)

# Gravity
func apply_gravity(delta: float) -> void:
	velocity.y += get_player_gravity() * delta
	if is_on_floor():
		jumps_left = extra_jumps

func get_player_gravity() -> float:
	return jump_gravity if velocity.y > 0.0 else fall_gravity

# Movement
func handle_movement(delta: float) -> void:
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	var direction := Vector3.ZERO
	if input_dir.length() > 0:
		var camera_yaw = horizontal_pivot.rotation.y
		var camera_basis = Basis(Vector3.UP, camera_yaw)
		direction = (camera_basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction != Vector3.ZERO:
		velocity.x = direction.x * speed
		velocity.z = direction.z * speed
		var target_yaw = atan2(-direction.x, -direction.z)
		rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, 10.0 * delta)
	else:
		velocity.x = move_toward(velocity.x, 0, speed)
		velocity.z = move_toward(velocity.z, 0, speed)

# Air Control
func handle_air_control(delta: float) -> void:
	if not is_on_floor():
		velocity.x = lerp(previous_velocity.x, velocity.x, 1.0 - exp(-air_control_lerp * delta))
		velocity.z = lerp(previous_velocity.z, velocity.z, 1.0 - exp(-air_control_lerp * delta))

# Input Handling
func _unhandled_input(event: InputEvent) -> void:
	handle_mouse_input(event)
	handle_camera_zoom(event)
	# Handle jump release for variable jump height
	if event.is_action_released("jump") and velocity.y >= 0:
		velocity.y *= 0.4

func handle_mouse_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_focus_next"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED else Input.MOUSE_MODE_CAPTURED
	if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED and event is InputEventMouseMotion:
		_look += -event.relative * mouse_sensitivity

func handle_jump_input() -> void:
	if Input.is_action_just_pressed("jump"):
		if is_on_floor() or coyote_timer > 0.0:
			velocity.y = jump_velocity
			coyote_timer = 0.0
		elif jumps_left > 0:
			jumps_left -= 1
			velocity.y = jump_velocity

func handle_camera_zoom(event: InputEvent) -> void:
	if event.is_action_pressed("scroll_forward"):
		camera_arm.spring_length = clampf(
			camera_arm.spring_length - camera_scroll_sensitivity,
			min_camera_distance, max_camera_distance)
	if event.is_action_pressed("scroll_backward"):
		camera_arm.spring_length = clampf(
			camera_arm.spring_length + camera_scroll_sensitivity,
			min_camera_distance, max_camera_distance)

func frame_camera_rotation() -> void:
	horizontal_pivot.rotation.y += _look.x
	vertical_pivot.rotation.x += _look.y
	vertical_pivot.rotation.x = clampf(
		vertical_pivot.rotation.x,
		deg_to_rad(min_camera_rotation),
		deg_to_rad(max_camera_rotation)
	)
	_look = Vector2.ZERO

#func look_toward_direction(direction: Vector3, delta: float) -> void:
