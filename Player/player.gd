extends CharacterBody3D

const SPEED := 5.0

@export_group("Jump Settings")
@export var jump_height: float = 5
@export var jump_time_to_peak: float = 0.5
@export var jump_time_to_descent: float = 0.25
@export var extra_jumps: int = 1

@export_group("Camera Settings")
@export var mouse_sensitivity: float = 0.00075
@export var camera_scroll_sensitivity = 0.25
@export var min_camera_rotation: float = -90
@export var max_camera_rotation: float = 90
@export var min_camera_distance: float = 1
@export var max_camera_distance: float = 15

var _look := Vector2.ZERO
var jumps_left = extra_jumps

@onready var horizontal_pivot: Node3D = $HorizontalPivot
@onready var vertical_pivot: Node3D = $HorizontalPivot/VerticalPivot
@onready var player_mesh: Node3D = $MeshInstance3D
@onready var collision_shape_3d: CollisionShape3D = $CollisionShape3D
@onready var camera_arm: SpringArm3D = $HorizontalPivot/VerticalPivot/CameraArm

# Jump vars
@onready var jump_velocity: float = (2.0 * jump_height) / jump_time_to_peak
@onready var jump_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_peak)
@onready var fall_gravity: float = (-2.0 * jump_height) / (jump_time_to_peak * jump_time_to_descent)

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _physics_process(delta: float) -> void:
	frame_camera_rotation()
	velocity.y += get_player_gravity() * delta

	# Jumping
	if is_on_floor():
		jumps_left = extra_jumps


	# Directional Movement
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	var direction := Vector3.ZERO
	if input_dir.length() > 0:
		var camera_yaw = horizontal_pivot.rotation.y
		var camera_basis = Basis(Vector3.UP, camera_yaw)
		direction = (camera_basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction != Vector3.ZERO:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
		var target_yaw = atan2(-direction.x, -direction.z)
		player_mesh.rotation.y = target_yaw
		collision_shape_3d.rotation.y = target_yaw
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	move_and_slide()

func _unhandled_input(event: InputEvent) -> void:
	# Toggle mouse capture
	if event.is_action_pressed("ui_focus_next"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED else Input.MOUSE_MODE_CAPTURED

	if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		if event is InputEventMouseMotion:
			_look += -event.relative * mouse_sensitivity

	if event.is_action_pressed("jump"):
		jump()
	if event.is_action_released("jump") and velocity.y >= 0:
		velocity.y *= 0.5
	# Camera zoom
	if Input.is_action_just_pressed("scroll_forward"):
		camera_arm.spring_length = clampf(camera_arm.spring_length - camera_scroll_sensitivity, min_camera_distance, max_camera_distance)
	if Input.is_action_just_pressed("scroll_backward"):
		camera_arm.spring_length = clampf(camera_arm.spring_length + camera_scroll_sensitivity, min_camera_distance, max_camera_distance)

func jump() -> void:
	if jumps_left > 0:
		jumps_left -= 1
		velocity.y = jump_velocity

func get_player_gravity() -> float:
	return jump_gravity if velocity.y > 0.0 else fall_gravity

func frame_camera_rotation() -> void:
	horizontal_pivot.rotation.y += _look.x
	vertical_pivot.rotation.x += _look.y
	vertical_pivot.rotation.x = clampf(
		vertical_pivot.rotation.x,
		deg_to_rad(min_camera_rotation),
		deg_to_rad(max_camera_rotation)
	)
	_look = Vector2.ZERO
