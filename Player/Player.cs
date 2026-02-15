using Godot;

// Player state machine for game logic (decoupled from animation state)
public enum PlayerState
{
	Idle,
	Running,
	Jumping,
	Dashing,
	Crouching,
	Sliding,
	LedgeGrabbing,
	Attacking
}

// Additional movement and abilities are added as separate nodes under this node/class.
// Remember to attach this node to subnodes mentioned above.
public partial class Player : CharacterBody3D
{
	[Signal]
	public delegate void VelocityCurrentEventHandler(Vector3 currentVelocity);

	[ExportGroup("Movement Settings")]
	[Export] public float MovementSpeed { get; set; } = 10.0f;
	[Export] public float PushForce = 60f;
	[Export] public float Acceleration { get; set; } = 30f;
	[Export] public float AirAcceleration { get; set; } = 10.0f;
	[Export] public float AirDrag { get; set; } = 0.5f;
	[Export] public float GroundFriction { get; set; } = 200f;

	public float AirTurnFaceRate = 10f;
	public float GroundTurnRate = 20f;

	[ExportGroup("Camera Settings")]
	[Export] public float MouseSensitivity { get; set; } = 0.00075f;
	[Export] public float CameraScrollSensitivity { get; set; } = 0.25f;
	[Export] public float MinCameraRotation { get; set; } = -90f;
	[Export] public float MaxCameraRotation { get; set; } = 90f;
	[Export] public float MinCameraDistance { get; set; } = 1f;
	[Export] public float MaxCameraDistance { get; set; } = 15f;

	[ExportGroup("Debug")]
	[Export] public bool DebugMode { get; set; } = false;

	public Vector3 Direction { get; set; } = Vector3.Zero;
	private Vector3 _horizontalVelocity = Vector3.Zero;

	// Game logic state (source of truth)
	public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
	public PlayerState PreviousState { get; private set; } = PlayerState.Idle;
	
	[Signal]
	public delegate void StateChangedEventHandler(PlayerState newState, PlayerState oldState);

	public Node3D PlayerMesh { get; private set; }
	public CollisionShape3D CollisionShape { get; private set; }
	public Node3D RigPivot { get; private set; }
	public Rig Rig { get; private set; }
	private Jump _jump;
	private LedgeGrab _ledgeGrab;
	private PlayerCamera _camera;

	public override void _Ready()
	{
		PlayerMesh = GetNode<Node3D>("RigPivot/Rig/CharacterRig/MeshInstance3D");
		CollisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		RigPivot = GetNode<Node3D>("RigPivot");
		Rig = GetNode<Rig>("RigPivot/Rig");
		_jump = GetNode<Jump>("Jump");
		_ledgeGrab = GetNode<LedgeGrab>("LedgeGrab");
		_camera = GetNode<PlayerCamera>("PlayerCamera");
	}

	public override void _PhysicsProcess(double delta)
	{
		ApplyGravity((float)delta);
		HandleMovement((float)delta);
		MoveAndSlide();

		// Apply hang position after MoveAndSlide if hanging on ledge
		_ledgeGrab?.ApplyHangPosition();

		// Push rigid bodies after movement.
		ApplyPushForce();

		// Update game state based on movement
		UpdatePlayerState();

		// Drive animations from game state
		Rig.UpdateFromGameState(CurrentState, Direction);

		EmitSignal(SignalName.VelocityCurrent, Velocity);
	}

	private void ApplyGravity(float delta)
	{
		if (CurrentState == PlayerState.Dashing)
		{
			Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
		}
		else if (CurrentState == PlayerState.LedgeGrabbing)
		{
			// Gravity is handled by LedgeGrab component
			// Don't apply normal gravity while hanging
		}
		else
		{
			Velocity = new Vector3(Velocity.X, Velocity.Y + GetPlayerGravity() * delta, Velocity.Z);
		}
	}

	public float GetPlayerGravity()
	{
		if (_jump != null)
		{
			return _jump.GetGravity(Velocity.Y);
		}
		else
		{
			GD.PrintErr("Using project settings gravity and not jump gravity!");
			return ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
		}
	}

	private Vector3 GetCameraMovementDirection()
	{
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		if (inputDir == Vector2.Zero)
			return Vector3.Zero;

		Basis cameraBasis = new Basis(Vector3.Up, _camera.HorizontalPivot.Rotation.Y);
		return (cameraBasis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
	}

	private void HandleMovement(float delta)
	{
		if (CurrentState == PlayerState.Dashing || CurrentState == PlayerState.Sliding || CurrentState == PlayerState.LedgeGrabbing)
			return;

		Direction = GetCameraMovementDirection();
		_horizontalVelocity = GetHorizontalVelocity();

		if (IsOnFloor())
		{
			_horizontalVelocity = ApplyGroundMovement(_horizontalVelocity, delta);
		}
		else
		{
			_horizontalVelocity = ApplyAirMovement(_horizontalVelocity, delta);
		}

		SetHorizontalVelocity(_horizontalVelocity);
	}

	private Vector3 GetHorizontalVelocity()
	{
		return new Vector3(Velocity.X, 0, Velocity.Z);
	}

	private void SetHorizontalVelocity(Vector3 horizontal)
	{
		Velocity = new Vector3(horizontal.X, Velocity.Y, horizontal.Z);
	}

	private void FaceDirection(float rate, float delta)
	{
		if (Direction == Vector3.Zero)
			return;

		float targetYaw = Mathf.Atan2(-Direction.X, -Direction.Z);
		RigPivot.Rotation = new Vector3(
			RigPivot.Rotation.X,
			Mathf.LerpAngle(RigPivot.Rotation.Y, targetYaw, rate * delta),
			RigPivot.Rotation.Z);
	}

	private Vector3 ApplyGroundMovement(Vector3 horizontal, float delta)
	{
		if (Direction != Vector3.Zero)
		{
			Vector3 target = Direction * MovementSpeed;
			horizontal = horizontal.MoveToward(target, Acceleration * delta);
			FaceDirection(GroundTurnRate, delta);
		}
		else
		{
			horizontal = horizontal.MoveToward(Vector3.Zero, GroundFriction * delta);
		}
		return horizontal;
	}

	private Vector3 ApplyAirMovement(Vector3 horizontal, float delta)
	{
		horizontal -= horizontal * AirDrag * delta;
		if (Direction != Vector3.Zero)
		{
			horizontal += Direction * AirAcceleration * delta;
			FaceDirection(AirTurnFaceRate, delta);
		}
		return horizontal;
	}

	// 
	private void ApplyPushForce()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision3D collision = GetSlideCollision(i);
			if (collision.GetCollider() is RigidBody3D rigidBody)
			{
				Vector3 normal = collision.GetNormal();

				// Only push if the collision is more horizontal than vertical
				// If normal.Y is close to 1, we're standing on top - don't push
				// If normal.Y is close to 0, it's a side collision - push it
				// Only push if collision is less than 60° from horizontal Lower values (e.g., 0.3f) = only push nearly horizontal collisions
				// Higher values (e.g., 0.7f) = push more collisions, even at steeper angles
				if (Mathf.Abs(normal.Y) < 0.5f)
				{
					Vector3 impulse = -normal * PushForce;
					rigidBody.ApplyCentralImpulse(impulse);
				}
			}
		}
	}

	
	// ============================================================
	// State Management System
	// ============================================================
	
	/// <summary>
	/// Sets the player's game logic state. This is the source of truth for gameplay logic.
	/// The Rig will read this state to drive animations.
	/// </summary>
	public void SetState(PlayerState newState)
	{
		if (CurrentState == newState)
			return;

		PreviousState = CurrentState;
		CurrentState = newState;
		EmitSignal(SignalName.StateChanged, (int)newState, (int)PreviousState);
	}

	/// <summary>
	/// Automatically determines and updates player state based on current conditions.
	/// Called every physics frame to keep state synchronized with gameplay.
	/// </summary>
	private void UpdatePlayerState()
	{
		// Don't auto-update certain states that are managed by ability components
		if (CurrentState == PlayerState.Dashing 
		    || CurrentState == PlayerState.Attacking 
		    || CurrentState == PlayerState.LedgeGrabbing)
		{
			return;
		}

		// Crouching and Sliding are managed by Crouch component
		if (CurrentState == PlayerState.Crouching || CurrentState == PlayerState.Sliding)
		{
			return;
		}

		// Auto-update based on ground state and movement
		if (!IsOnFloor())
		{
			// If airborne and not already jumping, transition to jumping
			// This handles falling off edges
			if (CurrentState != PlayerState.Jumping)
			{
				SetState(PlayerState.Jumping);
			}
			// Stay in Jumping state while airborne - don't check anything else
		}
		else if (IsOnFloor() && CurrentState == PlayerState.Jumping)
		{
			// Just landed from a jump - transition to ground movement
			Vector3 horizontalVel = new Vector3(Velocity.X, 0, Velocity.Z);
			
			if (horizontalVel.Length() > 0.1f || Direction.Length() > 0.1f)
			{
				SetState(PlayerState.Running);
			}
			else
			{
				SetState(PlayerState.Idle);
			}
		}
		else if (IsOnFloor() && CurrentState != PlayerState.Jumping)
		{
			// Normal ground movement state updates
			Vector3 horizontalVel = new Vector3(Velocity.X, 0, Velocity.Z);
			
			if (horizontalVel.Length() > 0.1f || Direction.Length() > 0.1f)
			{
				SetState(PlayerState.Running);
			}
			else
			{
				SetState(PlayerState.Idle);
			}
		}
	}

	/// <summary>
	/// Check if player is in a specific state.
	/// </summary>
	public bool IsInState(PlayerState state)
	{
		return CurrentState == state;
	}
}
