extends Control

@export var player: Player
@onready var velocity_label: Label = %VelocityLabel

func _ready() -> void:
    if player:
        player.VelocityCurrent.connect(_on_player_velocity_current)

func _on_player_velocity_current(current_velocity: Vector3) -> void:
    velocity_label.text = str(round(current_velocity.length() * 10) / 10)
