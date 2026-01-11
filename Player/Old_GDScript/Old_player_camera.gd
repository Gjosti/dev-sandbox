extends Node3D

@export var mouse_sensitivity: float = 0.00075
@export var camera_scroll_sensitivity: float = 0.25
@export var min_camera_rotation: float = -90
@export var max_camera_rotation: float = 90
@export var min_camera_distance: float = 1
@export var max_camera_distance: float = 15

var _look :Vector2 = Vector2.ZERO

@onready var horizontal_pivot: Node3D = $HorizontalPivot
@onready var vertical_pivot: Node3D = $HorizontalPivot/VerticalPivot
@onready var camera_arm: SpringArm3D = $HorizontalPivot/VerticalPivot/CameraArm

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _process(_delta: float) -> void:
	frame_camera_rotation()

func _unhandled_input(event: InputEvent) -> void:
	handle_mouse_input(event)
	handle_camera_zoom(event)

func handle_mouse_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_focus_next"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED else Input.MOUSE_MODE_CAPTURED
	if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED and event is InputEventMouseMotion:
		_look += -event.relative * mouse_sensitivity

func handle_camera_zoom(event: InputEvent) -> void:
	if event.is_action_pressed("scroll_forward"):
		camera_arm.spring_length = clampf(
			camera_arm.spring_length - camera_scroll_sensitivity,
			min_camera_distance, max_camera_distance)
	if event.is_action_pressed("scroll_backward"):
		camera_arm.spring_length = clampf(
			camera_arm.spring_length + camera_scroll_sensitivity,
			min_camera_distance, max_camera_distance)

func frame_camera_rotation() -> void:
	horizontal_pivot.rotation.y += _look.x
	vertical_pivot.rotation.x += _look.y
	vertical_pivot.rotation.x = clampf(
		vertical_pivot.rotation.x,
		deg_to_rad(min_camera_rotation),
		deg_to_rad(max_camera_rotation)
	)
	_look = Vector2.ZERO