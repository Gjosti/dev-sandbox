extends Node3D

@export var player: Player
@export var dash_cooldown:float = 0.5
@onready var timer: Timer = $Timer

var direction: Vector3 = Vector3.ZERO

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
# func _process(delta: float) -> void:
# 	pass


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("dash"):
		if timer.is_stopped():
			direction = -player.global_transform.basis.z.normalized()
			print("Dashable")
			timer.start(dash_cooldown)
		else:
			print("Not Dashable")
