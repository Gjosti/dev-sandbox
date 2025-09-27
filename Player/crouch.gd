extends Node3D

@export var player: Player
@export_group("Slide Properties")
@export var slide_cooldown: float = 0.5
@export var slide_duration: float = 0.3
@export var time_remaining: float = 0.0
@export var slide_threshold: float = 3

@onready var timer: Timer = $Timer

var stand_mesh_scale: Vector3 = Vector3(1, 1, 1)
var stand_height: float = 2.0
var crouch_mesh_scale: Vector3 = Vector3(1, 0.5, 1)
var crouch_height: float = 1.0
var is_crouching: bool = false



func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("crouch") and player.velocity.length() < slide_threshold:
		if not is_crouching:
			crouch()
	elif event.is_action_released("crouch"):
		if is_crouching:
			stand()

	if event.is_action_pressed("crouch") and player.velocity.length() > slide_threshold:
		slide()
		print("Sliding at " + str(player.velocity.length()) + " Velocity")
	


func crouch() -> void:
	is_crouching = true
	player.player_mesh.scale = crouch_mesh_scale
	var capsule := player.collision_shape_3d.shape as CapsuleShape3D
	player.collision_shape_3d.position.y = 0.5
	capsule.height = crouch_height
	player.rig.travel("Crouch")

func stand() -> void:
	is_crouching = false
	player.player_mesh.scale = stand_mesh_scale
	var capsule := player.collision_shape_3d.shape as CapsuleShape3D
	capsule.height = stand_height
	player.collision_shape_3d.position.y = 1
	player.rig.travel("MoveSpace")

# TODO
func slide() -> void:
	pass
