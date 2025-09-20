extends Node3D

# How to make and use Outlines in 3D | Godot - By Octodemy
# https://www.youtube.com/watch?v=CG0TMH8D8kY

@export var mesh: MeshInstance3D
@export var outline_material: Material
@export var selected_material: Material
@export var body: PhysicsBody3D = null

var selected: bool = false

func _ready():
	if body:
		body.mouse_entered.connect(_on_static_body_3d_mouse_entered)
		body.mouse_exited.connect(_on_static_body_3d_mouse_exited)
		body.input_event.connect(_on_static_body_3d_input_event)


func _on_static_body_3d_mouse_entered() -> void:
	if not selected:
		mesh.material_overlay = outline_material


func _on_static_body_3d_mouse_exited() -> void:
	if not selected:
		mesh.material_overlay = null

func _on_static_body_3d_input_event(_camera: Node, event: InputEvent, _event_position: Vector3, _normal: Vector3, _shape_idx: int) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and event.pressed and !event.is_echo():
			selected = not selected
			if selected:
				mesh.material_overlay = selected_material
			else:
				mesh.material_overlay = outline_material
