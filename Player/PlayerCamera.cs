using Godot;

public partial class PlayerCamera : Node3D
{
	[ExportGroup("Camera Settings")]
	[Export] public float MouseSensitivity { get; set; } = 0.00075f;
	[Export] public float CameraScrollSensitivity { get; set; } = 0.25f;
	[Export] public float MinCameraRotation { get; set; } = -90f;
	[Export] public float MaxCameraRotation { get; set; } = 90f;
	[Export] public float MinCameraDistance { get; set; } = 1f;
	[Export] public float MaxCameraDistance { get; set; } = 15f;

	private Vector2 _look = Vector2.Zero;

	public Node3D HorizontalPivot { get; private set; }
	private Node3D _verticalPivot;
	private SpringArm3D _cameraArm;

	public override void _Ready()
	{
		HorizontalPivot = GetNode<Node3D>("HorizontalPivot");
		_verticalPivot = GetNode<Node3D>("HorizontalPivot/VerticalPivot");
		_cameraArm = GetNode<SpringArm3D>("HorizontalPivot/VerticalPivot/CameraArm");

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Process(double delta)
	{
		FrameCameraRotation();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		HandleMouseInput(@event);
		HandleCameraZoom(@event);
	}

	private void HandleMouseInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_focus_next"))
		{
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
		}

		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
		{
			_look += -mouseMotion.Relative * MouseSensitivity;
		}
	}

	private void HandleCameraZoom(InputEvent @event)
	{
		if (@event.IsActionPressed("scroll_forward"))
		{
			_cameraArm.SpringLength = Mathf.Clamp(
				_cameraArm.SpringLength - CameraScrollSensitivity,
				MinCameraDistance,
				MaxCameraDistance);
		}

		if (@event.IsActionPressed("scroll_backward"))
		{
			_cameraArm.SpringLength = Mathf.Clamp(
				_cameraArm.SpringLength + CameraScrollSensitivity,
				MinCameraDistance,
				MaxCameraDistance);
		}
	}

	private void FrameCameraRotation()
	{
		HorizontalPivot.Rotation = new Vector3(
			HorizontalPivot.Rotation.X,
			HorizontalPivot.Rotation.Y + _look.X,
			HorizontalPivot.Rotation.Z);

		_verticalPivot.Rotation = new Vector3(
			Mathf.Clamp(
				_verticalPivot.Rotation.X + _look.Y,
				Mathf.DegToRad(MinCameraRotation),
				Mathf.DegToRad(MaxCameraRotation)),
			_verticalPivot.Rotation.Y,
			_verticalPivot.Rotation.Z);

		_look = Vector2.Zero;
	}
}
