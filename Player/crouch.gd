extends Node

@export var player: Player
@export var slide_threshold: float = 6.1
@export var slide_min_threshold: float = 3
@export var slide_friction: float = 0.985
@export var slide_turn_rate: float = 0.05

var stand_mesh_scale: Vector3 = Vector3(1, 1, 1)
var stand_height: float = 2.0
var crouch_mesh_scale: Vector3 = Vector3(1, 0.5, 1)
var crouch_height: float = 1.0
var velocity: Vector3 = Vector3.ZERO

# movement flags
var rotate_player_left: bool = false
var rotate_player_right: bool = false

func _physics_process(delta: float) -> void:
	if player.rig.IsSliding():
		apply_simple_slide(delta)
		if velocity.length() < slide_threshold:
			crouch()
	elif player.rig.IsCrouching() and velocity.length() > slide_threshold:
		slide()
	else:
		player.floor_stop_on_slope = true

func _unhandled_input(event: InputEvent) -> void:
	slide_movement(event)
	if event.is_action_pressed("crouch") and velocity.length() < slide_threshold:
		crouch()
	elif event.is_action_pressed("crouch") and velocity.length() > slide_threshold:
		print("trying to slide at ", velocity)
		slide()
	elif event.is_action_released("crouch"):
		stand()

func crouch() -> void:
	player.player_mesh.scale = crouch_mesh_scale
	var capsule: CapsuleShape3D = player.collision_shape_3d.shape as CapsuleShape3D
	player.collision_shape_3d.position.y = 0.5
	capsule.height = crouch_height
	player.rig.Travel("Crouch")

func stand() -> void:
	player.player_mesh.scale = stand_mesh_scale
	var capsule: CapsuleShape3D = player.collision_shape_3d.shape as CapsuleShape3D
	capsule.height = stand_height
	player.collision_shape_3d.position.y = 1
	player.rig.Travel("MoveSpace")

func slide() -> void:
	if player.rig.IsSliding():
		return
	if velocity.length() > slide_threshold:
		player.rig.Travel("Slide")

func slide_movement(event: InputEvent) -> void:
	if event.is_action_pressed("move_left"):
		rotate_player_left = true
	elif event.is_action_released("move_left"):
		rotate_player_left = false
	if event.is_action_pressed("move_right"):
		rotate_player_right = true
	elif event.is_action_released("move_right"):
		rotate_player_right = false


func apply_simple_slide(delta: float) -> void:
	player.floor_stop_on_slope = false

	# Rotation
	if rotate_player_left:
		player.rig_pivot.rotation.y += slide_turn_rate
	if rotate_player_right:
		player.rig_pivot.rotation.y -= slide_turn_rate

	# Apply friction and slope acceleration on ground
	if player.is_on_floor():
		velocity.x *= slide_friction
		velocity.z *= slide_friction

		var floor_normal: Vector3 = player.get_floor_normal()
		var gravity: float = player.get_player_gravity()
		var steepness: float = 1.0 - floor_normal.dot(Vector3.UP)
		var slope_dir: Vector3 = (floor_normal * Vector3.DOWN.dot(floor_normal) - Vector3.DOWN).normalized()
		var slope_accel: Vector3 = slope_dir * gravity * delta * pow(steepness, 0.58) * 10.0
		velocity += slope_accel

		# align horizontal velocity
		var horizontal_speed: float = Vector2(velocity.x, velocity.z).length()
		var facing_dir: Vector3 = - player.rig_pivot.global_transform.basis.z.normalized()
		velocity.x = facing_dir.x * horizontal_speed
		velocity.z = facing_dir.z * horizontal_speed

	# Apply gravity
	velocity.y += player.get_player_gravity() * delta
	player.velocity = velocity

func _on_player_velocity_current(current_velocity: Vector3) -> void:
	velocity = current_velocity
