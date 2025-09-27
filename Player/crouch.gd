extends Node3D

# Prob needs a Crouch MoveSpace to work in the AnimationTree

@export var player: Player
@export var crouch_modifier: float = 0.5
@export var crouch_camera_offset: float = 0.5

var is_crouching: bool = false

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("crouch"):
		if not is_crouching:
			is_crouching = true
			player.player_mesh.rotation = Vector3(crouch_modifier , 1, 1)
			player.collision_shape_3d.rotation = Vector3(crouch_modifier, 1, 1)
	else:
		if is_crouching:
			is_crouching = false
			player.player_mesh.rotation = Vector3(1, 1, 1)
			player.collision_shape_3d.rotation = Vector3(1, 1, 1)
