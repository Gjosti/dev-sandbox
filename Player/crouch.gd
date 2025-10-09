extends Node

@export var player: Player
@export var slide_threshold: float = 3

var stand_mesh_scale: Vector3 = Vector3(1, 1, 1)
var stand_height: float = 2.0
var crouch_mesh_scale: Vector3 = Vector3(1, 0.5, 1)
var crouch_height: float = 1.0
var velocity: Vector3 = Vector3.ZERO

func _physics_process(_delta):
	if player.rig.is_sliding() and velocity.length() < slide_threshold:
		stand()
		crouch()
	elif player.rig.is_crouching() and velocity.length() > slide_threshold:
		slide()

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("crouch") and velocity.length() < slide_threshold:
		crouch()
	elif event.is_action_pressed("crouch") and velocity.length() > slide_threshold:
		print("trying to slide at ", velocity)
		slide()
	elif event.is_action_released("crouch"):
		stand()

func crouch() -> void:
	if player.rig.is_crouching():
		return
	player.player_mesh.scale = crouch_mesh_scale
	var capsule := player.collision_shape_3d.shape as CapsuleShape3D
	player.collision_shape_3d.position.y = 0.5
	capsule.height = crouch_height
	player.rig.travel("Crouch")

func stand() -> void:
	if player.rig.is_crouching() or player.rig.is_sliding():
		player.player_mesh.scale = stand_mesh_scale
		var capsule := player.collision_shape_3d.shape as CapsuleShape3D
		capsule.height = stand_height
		player.collision_shape_3d.position.y = 1
		player.rig.travel("MoveSpace")

func slide() -> void:
	if player.rig.is_sliding():
		return
	if velocity.length() > slide_threshold:
		player.rig.travel("Slide")
	

func _on_player_velocity_current(current_velocity: Vector3) -> void:
	velocity = current_velocity
	
