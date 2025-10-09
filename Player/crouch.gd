extends Node

@export var player: Player

var stand_mesh_scale: Vector3 = Vector3(1, 1, 1)
var stand_height: float = 2.0
var crouch_mesh_scale: Vector3 = Vector3(1, 0.5, 1)
var crouch_height: float = 1.0


func _unhandled_input(event: InputEvent) -> void:
	# Simple crouching
	if event.is_action_pressed("crouch"):
		crouch()
	elif event.is_action_released("crouch"):
		stand()

func crouch() -> void:
	player.player_mesh.scale = crouch_mesh_scale
	var capsule := player.collision_shape_3d.shape as CapsuleShape3D
	player.collision_shape_3d.position.y = 0.5
	capsule.height = crouch_height
	player.rig.travel("Crouch")

func stand() -> void:
	if player.rig.is_crouching():
		player.player_mesh.scale = stand_mesh_scale
		var capsule := player.collision_shape_3d.shape as CapsuleShape3D
		capsule.height = stand_height
		player.collision_shape_3d.position.y = 1
		player.rig.travel("MoveSpace")

