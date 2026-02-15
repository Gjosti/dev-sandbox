using Godot;

public partial class Crouch : Node
{
	[Export] public Player Player { get; set; }
	[Export] public float CrouchMovementModifier { get; set; }
	[Export] public float SlideThreshold { get; set; } = 10.1f;
	[Export] public float SlideMinThreshold { get; set; } = 3f;
	[Export] public float SlideFriction { get; set; } = 0.985f;
	[Export] public float SlideTurnRate { get; set; } = 0.05f;

	private readonly Vector3 _standMeshScale = new(1, 1, 1);
	private const float StandHeight = 2.0f;
	private readonly Vector3 _crouchMeshScale = new(1, 0.5f, 1);
	private const float CrouchHeight = 1.0f;
	private Vector3 _velocity = Vector3.Zero;

	private bool _rotatePlayerLeft = false;
	private bool _rotatePlayerRight = false;

	private Node3D _rigPivot;
	private Node3D _playerMesh;
	private CollisionShape3D _collisionShape;

	public override void _Ready()
	{
	    if (Player != null)
	    {
	        Player.VelocityCurrent += OnPlayerVelocityCurrent;
	        
	        // Initialize references immediately
	        _rigPivot = Player.GetNode<Node3D>("RigPivot");
	        _playerMesh = Player.GetNode<Node3D>("RigPivot/Rig/CharacterRig/MeshInstance3D");
	        _collisionShape = Player.GetNode<CollisionShape3D>("CollisionShape3D");
	    }
	}

	public override void _PhysicsProcess(double delta)
	{
	    if (Player == null) return;
	    
	    float velocityLength = _velocity.Length();

	    if (Player.CurrentState == PlayerState.Sliding)
	    {
	        ApplySimpleSlide((float)delta);
	        if (velocityLength < SlideMinThreshold)
	            PerformCrouch();
	    }
	    else if (Player.CurrentState == PlayerState.Crouching && velocityLength > SlideThreshold)
	        Slide();
	    else
	        Player.FloorStopOnSlope = true;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Don't handle crouch input during ledge grab (LedgeGrab uses it for release)
		if (Player.CurrentState == PlayerState.LedgeGrabbing)
			return;

		SlideMovement(@event);

		if (@event.IsActionPressed("crouch") && _velocity.Length() < SlideThreshold)
		{
			PerformCrouch();
		}
		else if (@event.IsActionPressed("crouch") && _velocity.Length() > SlideThreshold)
		{
			GD.Print("trying to slide at ", _velocity);
			Slide();
		}
		else if (@event.IsActionReleased("crouch"))
		{
			Stand();
		}
	}

	private void PerformCrouch()
	{
		_playerMesh.Scale = _crouchMeshScale;
		var capsule = (CapsuleShape3D)_collisionShape.Shape;
		_collisionShape.Position = new Vector3(_collisionShape.Position.X, 0.5f, _collisionShape.Position.Z);
		capsule.Height = CrouchHeight;
		Player.SetState(PlayerState.Crouching);
	}

	private void Stand()
	{
		_playerMesh.Scale = _standMeshScale;
		var capsule = (CapsuleShape3D)_collisionShape.Shape;
		capsule.Height = StandHeight;
		_collisionShape.Position = new Vector3(_collisionShape.Position.X, 1f, _collisionShape.Position.Z);
		// Transition back to idle/running - let UpdatePlayerState determine which
		Player.SetState(PlayerState.Idle);
	}

	private void Slide()
	{
		if (Player.CurrentState == PlayerState.Sliding) return;
		if (_velocity.Length() > SlideThreshold)
		{
			Player.SetState(PlayerState.Sliding);
		}
	}

	private void SlideMovement(InputEvent @event)
	{
		if (@event.IsActionPressed("move_left"))
			_rotatePlayerLeft = true;
		else if (@event.IsActionReleased("move_left"))
			_rotatePlayerLeft = false;

		if (@event.IsActionPressed("move_right"))
			_rotatePlayerRight = true;
		else if (@event.IsActionReleased("move_right"))
			_rotatePlayerRight = false;
	}

	private void ApplySimpleSlide(float delta)
	{
		Player.FloorStopOnSlope = false;

		if (_rotatePlayerLeft)
			_rigPivot.Rotation = new Vector3(_rigPivot.Rotation.X, _rigPivot.Rotation.Y + SlideTurnRate, _rigPivot.Rotation.Z);
		if (_rotatePlayerRight)
			_rigPivot.Rotation = new Vector3(_rigPivot.Rotation.X, _rigPivot.Rotation.Y - SlideTurnRate, _rigPivot.Rotation.Z);

		if (Player.IsOnFloor())
		{
			_velocity.X *= SlideFriction;
			_velocity.Z *= SlideFriction;

			Vector3 floorNormal = Player.GetFloorNormal();
			float gravity = (float)Player.GetPlayerGravity();
			float steepness = 1.0f - floorNormal.Dot(Vector3.Up);
			Vector3 slopeDir = (floorNormal * Vector3.Down.Dot(floorNormal) - Vector3.Down).Normalized();
			Vector3 slopeAccel = slopeDir * gravity * delta * Mathf.Pow(steepness, 0.58f) * 10.0f;
			_velocity += slopeAccel;

			float horizontalSpeed = new Vector2(_velocity.X, _velocity.Z).Length();
			Vector3 facingDir = -_rigPivot.GlobalTransform.Basis.Z.Normalized();
			_velocity.X = facingDir.X * horizontalSpeed;
			_velocity.Z = facingDir.Z * horizontalSpeed;
		}

		_velocity.Y += Player.GetPlayerGravity() * delta;
		Player.Velocity = _velocity;
	}

	private void OnPlayerVelocityCurrent(Vector3 currentVelocity)
	{
	    _velocity = currentVelocity;
	}
}
