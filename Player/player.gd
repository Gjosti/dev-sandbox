extends CharacterBody3D
class_name Player

# Movement Settings
@export_group("Movement Settings")
@export var speed: float = 6.0
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

# State Variables
var direction: Vector3 = Vector3.ZERO

# Node References
@onready var player_mesh: Node3D = $RigPivot/Rig/CharacterRig/MeshInstance3D
@onready var collision_shape_3d: CollisionShape3D = $CollisionShape3D
@onready var rig_pivot: Node3D = $RigPivot
@onready var rig: Node3D = $RigPivot/Rig
@onready var jump: Node = $Jump 
@onready var camera := $PlayerCamera

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
	if input_dir == Vector2.ZERO:
		return Vector3.ZERO
	var camera_basis := Basis(Vector3.UP, camera.horizontal_pivot.rotation.y)
	return (camera_basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()

func handle_movement(delta: float) -> void:
	if rig.is_dashing() or rig.is_sliding():
		return
	direction = get_camera_movement_direction()
	if is_on_floor():
		if direction != Vector3.ZERO:
			velocity.x = move_toward(velocity.x, direction.x * speed, acceleration * delta)
			velocity.z = move_toward(velocity.z, direction.z * speed, acceleration * delta)
			var target_yaw = atan2(-direction.x, -direction.z)
			rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, ground_turn_rate * delta)
		else:
			# Slow down to zero horizontal velocity based on friction when no movement direction input
			velocity.z = move_toward(velocity.z, 0, ground_friction * delta)
			velocity.x = move_toward(velocity.x, 0, ground_friction * delta)
	else:
		# Apply air drag while in air
		velocity.x -= velocity.x * air_drag * delta
		velocity.z -= velocity.z * air_drag * delta

		# Add acceleration in input direction
		if direction != Vector3.ZERO:
			var input_accel = direction * air_acceleration * delta
			velocity.x += input_accel.x
			velocity.z += input_accel.z
			
			var target_yaw = atan2(-direction.x, -direction.z)
			rig_pivot.rotation.y = lerp_angle(rig_pivot.rotation.y, target_yaw, air_turn_face_rate * delta)

	rig.update_animation_tree(direction)

# Input Handling
func _unhandled_input(event: InputEvent) -> void:
	handle_attack_input(event)




func handle_attack_input(event: InputEvent) -> void:
	if rig.is_idle():
		if event.is_action_pressed("attack"):
			main_action()

func main_action() -> void:
	rig.travel("Attack")

