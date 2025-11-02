extends Node3D

# If ledge is detected then set velocity to zero. 
# ledge is detected if: top area  does not detect collision whil bottom does.

@export var player: Player

@onready var top_area: Area3D = $Area3D_Top
@onready var bottom_area: Area3D = $Area3D_Bottom

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
