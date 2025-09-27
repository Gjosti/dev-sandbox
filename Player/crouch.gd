extends Node3D

@export var player: Player
@export var crouch_mesh_scale: Vector3 = Vector3(1, 0.5, 1)
@export var stand_mesh_scale: Vector3 = Vector3(1, 1, 1)
@export var crouch_height: float = 1.0
@export var stand_height: float = 2.0

var is_crouching: bool = false

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("crouch"):
		if not is_crouching:
			is_crouching = true
			player.player_mesh.scale = crouch_mesh_scale
			var capsule := player.collision_shape_3d.shape as CapsuleShape3D
			player.collision_shape_3d.position.y = 0.5
			capsule.height = crouch_height
			player.rig.travel("Crouch")
	elif event.is_action_released("crouch"):
		if is_crouching:
			is_crouching = false
			player.player_mesh.scale = stand_mesh_scale
			var capsule := player.collision_shape_3d.shape as CapsuleShape3D
			capsule.height = stand_height
			player.collision_shape_3d.position.y = 1
			player.rig.travel("MoveSpace")
