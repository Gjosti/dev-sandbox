extends Node3D

@export var player: Player
@export var crouch_pitch: float = -45.0 # Degrees to lean forward

var is_crouching: bool = false

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("crouch"):
		if not is_crouching:
			is_crouching = true
			var pitch_rad = deg_to_rad(crouch_pitch)
			player.player_mesh.rotation.x = pitch_rad
			player.collision_shape_3d.rotation.x = pitch_rad
	else:
		if is_crouching:
			is_crouching = false
			player.player_mesh.rotation.x = 0.0
			player.collision_shape_3d.rotation.x = 0.0
