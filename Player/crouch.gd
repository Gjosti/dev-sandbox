extends Node3D

@export var player: Player
@export_group("Slide Properties")
@export var slide_cooldown: float = 0.5
@export var time_remaining: float = 0.0
@export var slide_threshold: float = 1

@onready var timer: Timer = $Timer

var stand_mesh_scale: Vector3 = Vector3(1, 1, 1)
var stand_height: float = 2.0
var crouch_mesh_scale: Vector3 = Vector3(1, 0.5, 1)
var crouch_height: float = 1.0
var slide_friction: float = 0.2

func _physics_process(delta):
	if player.rig.is_sliding():
		# Apply ice-like friction
		player.velocity.x = lerp(player.velocity.x, 0.0, slide_friction * delta)
		player.velocity.z = lerp(player.velocity.z, 0.0, slide_friction * delta)
		# # Cancel slide if velocity is below threshold
		# if player.velocity.length() < slide_threshold:
		# 	stand()

func _unhandled_input(event: InputEvent) -> void:
	# Simple crouching
	if event.is_action_pressed("crouch") and player.velocity.length() < slide_threshold:
		if not player.rig.is_sliding():
			crouch()
	elif event.is_action_released("crouch"):
		if player.rig.is_sliding() or player.rig.is_crouching():
			stand()

	# Slide
	if event.is_action_pressed("crouch") and player.velocity.length() > slide_threshold:
					slide()
	elif event.is_action_released("crouch"): 
		if player.rig.is_sliding():
			stand()

	if event.is_action_pressed("crouch") and player.velocity.length() > slide_threshold and player.is_on_floor():
		slide()
		print("Sliding at " + str(player.velocity.length()) + " Velocity")
	


func crouch() -> void:
	
	player.player_mesh.scale = crouch_mesh_scale
	var capsule := player.collision_shape_3d.shape as CapsuleShape3D
	player.collision_shape_3d.position.y = 0.5
	capsule.height = crouch_height
	player.rig.travel("Crouch")

func stand() -> void:
	
	player.player_mesh.scale = stand_mesh_scale
	var capsule := player.collision_shape_3d.shape as CapsuleShape3D
	capsule.height = stand_height
	player.collision_shape_3d.position.y = 1
	player.rig.travel("MoveSpace")

# TODO
func slide() -> void:
	pass
	# is_sliding = true;
