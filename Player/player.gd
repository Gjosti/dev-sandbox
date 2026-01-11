extends CharacterBody3D
class_name Player

# Movement Settings
@export_group("Movement Settings")
@export var movement_speed: float = 6.0
@export var crouch_movement_modifier: float
@export var acceleration: float = 30
@export var air_acceleration: float = 10.0
@export var air_drag: float = 0.5 
@export var ground_friction: float = 200 # 375 is instant stop, however lesser values also could feel instant depending on speed
var air_turn_face_rate: float = 10
var ground_turn_rate: float = 20

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

# Variables
var direction: Vector3 = Vector3.ZERO
var horizontal_velocity: Vector3 = Vector3.ZERO

# Node References
@onready var player_mesh: Node3D = $RigPivot/Rig/CharacterRig/MeshInstance3D
@onready var collision_shape_3d: CollisionShape3D = $CollisionShape3D
@onready var rig_pivot: Node3D = $RigPivot
@onready var rig: Node3D = $RigPivot/Rig
@onready var jump: Node = $Jump 
@onready var camera: Node3D = $PlayerCamera

func _ready() -> void:
	pass

func _physics_process(delta: float) -> void:
	apply_gravity(delta)
	handle_movement(delta)
	move_and_slide()

	# Share velocity to children nodes/abilities such as sliding.
	emit_signal("velocity_current", velocity)

# Gravity
func apply_gravity(delta: float) -> void:
	if rig.IsDashing():
		velocity.y = 0
	else:
		velocity.y += get_player_gravity() * delta

func get_player_gravity() -> float:
	if jump:
		return jump.GetGravity(velocity.y)
	else:
		printerr("Using project settings gravity and not jump gravity!")
		return get_gravity().length()

# Movement
func get_camera_movement_direction() -> Vector3:
	var input_dir: Vector2 = Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	if input_dir == Vector2.ZERO:
		return Vector3.ZERO
	var camera_basis: Basis = Basis(Vector3.UP, camera.HorizontalPivot.rotation.y)
	return (camera_basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()

func handle_movement(delta: float) -> void:
	if rig.IsDashing() or rig.IsSliding():
		return
	direction = get_camera_movement_direction()
	horizontal_velocity = _get_horizontal_velocity()

	if is_on_floor():
		horizontal_velocity = _apply_ground_movement(horizontal_velocity, delta)
	else:
		horizontal_velocity = _apply_air_movement(horizontal_velocity, delta)

	_set_horizontal_velocity(horizontal_velocity)
	rig.UpdateAnimationTree(direction)

# Helpers
func _get_horizontal_velocity() -> Vector3:
	return Vector3(velocity.x, 0, velocity.z)

func _set_horizontal_velocity(horizontal: Vector3) -> void:
	velocity.x = horizontal.x
	velocity.z = horizontal.z

func _face_direction(rate: float, delta: float) -> void:
	if direction == Vector3.ZERO:
		return
	var target_yaw: float= atan2(-direction.x, -direction.z)
	rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, rate * delta)

func _apply_ground_movement(horizontal: Vector3, delta: float) -> Vector3:
	if direction != Vector3.ZERO:
		var target: Vector3 = direction * movement_speed
		horizontal = horizontal.move_toward(target, acceleration * delta)
		_face_direction(ground_turn_rate, delta)
	else:
		horizontal = horizontal.move_toward(Vector3.ZERO, ground_friction * delta)
	return horizontal

func _apply_air_movement(horizontal: Vector3, delta: float) -> Vector3:
	horizontal -= horizontal * air_drag * delta
	if direction != Vector3.ZERO:
		horizontal += direction * air_acceleration * delta
		_face_direction(air_turn_face_rate, delta)
	return horizontal

# Input Handling
func _unhandled_input(event: InputEvent) -> void:
	handle_attack_input(event)




func handle_attack_input(event: InputEvent) -> void:
	if rig.IsIdle():
		if event.is_action_pressed("attack"):
			main_action()

func main_action() -> void:
	rig.Travel("Attack")
