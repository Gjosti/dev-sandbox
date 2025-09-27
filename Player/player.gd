extends CharacterBody3D
class_name Player

# Movement Settings
@export_group("Movement Settings")
@export var speed: float = 6.0
@export var air_control_lerp: float = 75 # Controls air movement responsiveness
var air_turn_face_rate: float = 2.5

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
var previous_velocity: Vector3
var coyote_time: float = 0.1
var coyote_timer: float = 0.0
var direction: Vector3 = Vector3.ZERO

# Camera
var camera_yaw: float
var camera_basis: Basis
var target_yaw: float


# Node References
@onready var horizontal_pivot: Node3D = $HorizontalPivot
@onready var vertical_pivot: Node3D = $HorizontalPivot/VerticalPivot
@onready var player_mesh: Node3D = $RigPivot/Rig/CharacterRig/MeshInstance3D
@onready var collision_shape_3d: CollisionShape3D = $CollisionShape3D
@onready var rig_pivot: Node3D = $RigPivot
@onready var rig: Node3D = $RigPivot/Rig
@onready var camera_arm: SpringArm3D = $HorizontalPivot/VerticalPivot/CameraArm
@onready var jump: Node = $Jump # or the correct path to your jump node

# Initialization
func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

# Physics Processing
func _physics_process(delta: float) -> void:
	previous_velocity = velocity
	frame_camera_rotation()
	apply_gravity(delta)
	handle_movement(delta)
	# handle_air_control(delta)
	move_and_slide()

	 # Coyote time 
	if is_on_floor():
		coyote_timer = coyote_time
	else:
		coyote_timer = max(0.0, coyote_timer - delta)

# Gravity
func apply_gravity(delta: float) -> void:
	if rig.is_dashing():
		velocity.y = 0
	else:
		velocity.y += get_player_gravity() * delta

func get_player_gravity() -> float:
	return jump.get_gravity(velocity.y)

# Movement
func handle_movement(delta: float) -> void:
	if rig.is_dashing():
		return
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	direction = Vector3.ZERO
	if input_dir.length() > 0:
		camera_yaw = horizontal_pivot.rotation.y
		camera_basis = Basis(Vector3.UP, camera_yaw)
		direction = (camera_basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if is_on_floor():
		if direction != Vector3.ZERO:
			velocity.x = direction.x * speed
			velocity.z = direction.z * speed
			target_yaw = atan2(-direction.x, -direction.z)
			rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, 10.0 * delta)
		elif velocity.y == 0.0:
			velocity.x = move_toward(velocity.x, 0, speed)
			velocity.z = move_toward(velocity.z, 0, speed)
	else:
		# In air, accumulate velocity instead of resetting
		if direction != Vector3.ZERO:
			velocity.x += direction.x * speed * (air_control_lerp) * delta
			velocity.z += direction.z * speed * (air_control_lerp) * delta
			target_yaw = atan2(-direction.x, -direction.z)
			rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, air_turn_face_rate * delta)
	rig.update_animation_tree(direction)

# TODO Air Control TODO REDO! NOT IN USE
func handle_air_control(delta: float) -> void:
	if not is_on_floor():
		var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
		if input_dir.length() > 0:
			camera_yaw = horizontal_pivot.rotation.y
			camera_basis = Basis(Vector3.UP, camera_yaw)
			var air_direction = (camera_basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
			# Always add a small impulse in the input direction, even from zero speed
			var air_impulse = air_direction * speed * (air_control_lerp / 100.0) * delta
			velocity.x += air_impulse.x
			velocity.z += air_impulse.z

# Input Handling
func _unhandled_input(event: InputEvent) -> void:
	handle_mouse_input(event)
	handle_camera_zoom(event)
	handle_attack_input(event)
	# Handle jump release for variable jump height
	if event.is_action_released("jump") and velocity.y >= 0:
		velocity.y *= 0.4

func handle_mouse_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_focus_next"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED else Input.MOUSE_MODE_CAPTURED
	if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED and event is InputEventMouseMotion:
		_look += -event.relative * mouse_sensitivity


func handle_attack_input(event: InputEvent) -> void:
	if rig.is_idle():
		if event.is_action_pressed("attack"):
			main_action()


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

func main_action() -> void:
	rig.travel("Attack")
