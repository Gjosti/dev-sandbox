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

# Signals
signal velocity_current(current_velocity: Vector3) #The player node is emitting it's velocity for children nodes to use. such as crouch's slide function

# State Variables
var direction: Vector3 = Vector3.ZERO

# Node References
@onready var player_mesh: Node3D = $RigPivot/Rig/CharacterRig/MeshInstance3D
@onready var collision_shape_3d: CollisionShape3D = $CollisionShape3D
@onready var rig_pivot: Node3D = $RigPivot
@onready var rig: Node3D = $RigPivot/Rig
@onready var jump: Node = $Jump 

# Initialization
func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

# Physics Processing
func _physics_process(delta: float) -> void:
	apply_gravity(delta)
	handle_movement(delta)
	move_and_slide()

	# Share velocity to children nodes/abilities such as sliding.
	emit_signal("velocity_current", velocity)

# Gravity
func apply_gravity(delta: float) -> void:
	if rig.is_dashing():
		velocity.y = 0
	else:
		velocity.y += get_player_gravity() * delta

func get_player_gravity() -> float:
	if jump:
		return jump.get_gravity(velocity.y)
	else:
		printerr("Using project settings gravity and not jump gravity!")
		return get_gravity().length()

# Movement
func get_camera_movement_direction() -> Vector3:
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	if input_dir.length() > 0:
		# Get yaw from camera node instead
		var camera_node := get_node("PlayerCamera/HorizontalPivot")
		var camera_yaw = camera_node.rotation.y
		var camera_basis = Basis(Vector3.UP, camera_yaw)
		return (camera_basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	return Vector3.ZERO

func handle_movement(delta: float) -> void:
	if rig.is_dashing() or rig.is_sliding():
		return
	direction = get_camera_movement_direction()
	if is_on_floor():
		if direction != Vector3.ZERO:
			velocity.x = direction.x * speed
			velocity.z = direction.z * speed
			var target_yaw = atan2(-direction.x, -direction.z)
			rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, 10.0 * delta)
		elif velocity.y == 0.0:
			velocity.x = move_toward(velocity.x, 0, speed)
			velocity.z = move_toward(velocity.z, 0, speed)
	else:
		if direction != Vector3.ZERO:
			velocity.x += direction.x * speed * (air_control_lerp) * delta
			velocity.z += direction.z * speed * (air_control_lerp) * delta
			var target_yaw = atan2(-direction.x, -direction.z)
			rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, air_turn_face_rate * delta)
	rig.update_animation_tree(direction)

# Input Handling
func _unhandled_input(event: InputEvent) -> void:
	handle_mouse_input(event)
	handle_attack_input(event)
	# Handle jump release for variable jump height
	if event.is_action_released("jump") and velocity.y >= 0:
		velocity.y *= 0.4

func handle_mouse_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_focus_next"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED else Input.MOUSE_MODE_CAPTURED


func handle_attack_input(event: InputEvent) -> void:
	if rig.is_idle():
		if event.is_action_pressed("attack"):
			main_action()

func main_action() -> void:
	rig.travel("Attack")

func _on_slide_friction_applied(new_velocity: Vector3) -> void:
	velocity.x = new_velocity.x
	velocity.z = new_velocity.z
