extends Control

@onready var velocity_label: Label = %VelocityLabel



func _on_player_velocity_current(current_velocity: Vector3) -> void:
	velocity_label.text = str(round(current_velocity.length() * 10) / 10)
