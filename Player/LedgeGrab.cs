using Godot;

public partial class LedgeGrab : Node3D
{
	[ExportGroup("References")]
	[Export] public Player Player { get; set; }
	
	[ExportGroup("Detection Settings")]
	[Export] public float ForwardDetectionDistance { get; set; } = 0.6f;
	[Export] public float LedgeCheckHeightOffset { get; set; } = 1.8f; // Height from player base to check for ledge
	[Export] public float WallCheckHeightOffset { get; set; } = 1.2f; // Lower check for wall detection
	[Export] public float LedgeThickness { get; set; } = 0.3f; // How thick the ledge surface can be
	[Export] public float MinimumFallSpeed { get; set; } = -2.0f; // Must be falling to grab
	
	[ExportGroup("Grab Position")]
	[Export] public float HangOffsetFromWall { get; set; } = 0.45f; // How far from wall to position player
	[Export] public float HangHeightBelowLedge { get; set; } = 0.8f; // How far below ledge player's hands hang
	[Export] public float PlayerReachHeight { get; set; } = 1.7f; // Height from player feet to hands when raised
	
	[ExportGroup("Hang Settings")]
	[Export] public bool AllowHangRelease { get; set; } = true; // Can press crouch to let go
	
	[ExportGroup("Climb Settings")]
	[Export] public float ClimbJumpHeight { get; set; } = 2.0f;
	
	// Internal state
	private bool _isHanging = false;
	private Vector3 _ledgePosition = Vector3.Zero;
	private Vector3 _ledgeNormal = Vector3.Zero;
	private Vector3 _cachedWallNormal = Vector3.Zero; // Cached normalized wall normal
	private PhysicsRayQueryParameters3D _rayParams;
	private PhysicsDirectSpaceState3D _spaceState;

	public override void _Ready()
	{
		// Initialize ray parameters for reuse
		_rayParams = new PhysicsRayQueryParameters3D();
		_rayParams.CollideWithAreas = false;
		_rayParams.CollideWithBodies = true;
		
		// Cache space state
		_spaceState = Player.GetWorld3D().DirectSpaceState;
		
		// Subscribe to state changes to optimize when we check for ledges
		Player.StateChanged += OnPlayerStateChanged;
		
		// Start disabled, only check when in air
		SetPhysicsProcess(false);
	}

	private void OnPlayerStateChanged(PlayerState newState, PlayerState oldState)
	{
		// Only run physics process when jumping/falling or hanging
		bool shouldProcess = newState == PlayerState.Jumping || newState == PlayerState.LedgeGrabbing;
		SetPhysicsProcess(shouldProcess);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isHanging)
		{
			// Hanging state handled by ApplyHangPosition called from Player
			return;
		}
		
		CheckForLedge();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isHanging)
			return;
			
		// Handle input only when hanging
		if (AllowHangRelease && @event.IsActionPressed("crouch"))
		{
			ReleaseHang();
		}
		else if (@event.IsActionPressed("jump"))
		{
			PerformClimb();
		}
	}

	private void CheckForLedge()
	{
		// Only check when airborne and falling
		if (Player.IsOnFloor() || Player.Velocity.Y > MinimumFallSpeed)
			return;

		// Don't interrupt certain states
		if (Player.CurrentState == PlayerState.Dashing || 
		    Player.CurrentState == PlayerState.Attacking)
			return;

		// Get player's facing direction (from RigPivot rotation)
		Node3D rigPivot = Player.RigPivot;
		Vector3 forwardDirection = -rigPivot.GlobalTransform.Basis.Z;
		forwardDirection.Y = 0;
		forwardDirection = forwardDirection.Normalized();

		// Perform wall check at chest height
		Vector3 wallCheckStart = Player.GlobalPosition + Vector3.Up * WallCheckHeightOffset;
		Vector3 wallCheckEnd = wallCheckStart + forwardDirection * ForwardDetectionDistance;
		
		var wallHit = CastRay(wallCheckStart, wallCheckEnd);
		if (wallHit.Count == 0)
			return;

		// Store wall normal (pointing away from wall toward player)
		Vector3 wallNormal = (Vector3)wallHit["normal"];

		// Check for ledge (no wall at head height)
		Vector3 ledgeCheckStart = Player.GlobalPosition + Vector3.Up * LedgeCheckHeightOffset;
		Vector3 ledgeCheckEnd = ledgeCheckStart + forwardDirection * ForwardDetectionDistance;
		
		var ledgeHit = CastRay(ledgeCheckStart, ledgeCheckEnd);
		if (ledgeHit.Count > 0)
			return; // Still wall above, not a ledge

		// Found a ledge! Now find the exact top position
		// Cast down from above the ledge check point
		Vector3 topCheckStart = ledgeCheckEnd + Vector3.Up * LedgeThickness;
		Vector3 topCheckEnd = topCheckStart + Vector3.Down * (LedgeThickness * 2);
		
		var topHit = CastRay(topCheckStart, topCheckEnd);
		if (topHit.Count == 0)
			return; // No ledge top found

		// We found a valid ledge!
		_ledgePosition = (Vector3)topHit["position"];
		_ledgeNormal = wallNormal; // Store the wall normal, not the ledge top normal
		
		StartHanging();
	}

	private Godot.Collections.Dictionary CastRay(Vector3 from, Vector3 to)
	{
		_rayParams.From = from;
		_rayParams.To = to;
		_rayParams.Exclude = new Godot.Collections.Array<Rid> { Player.GetRid() };
		
		return _spaceState.IntersectRay(_rayParams);
	}

	private void StartHanging()
	{
		_isHanging = true;
		
		// Cache normalized wall normal for reuse
		_cachedWallNormal = _ledgeNormal;
		_cachedWallNormal.Y = 0;
		_cachedWallNormal = _cachedWallNormal.Normalized();
		
		Vector3 hangPosition = _ledgePosition;
		hangPosition += _cachedWallNormal * HangOffsetFromWall;
		hangPosition.Y = _ledgePosition.Y - PlayerReachHeight + HangHeightBelowLedge;
		
		// Snap player to hang position
		Player.GlobalPosition = hangPosition;
		Player.Velocity = Vector3.Zero;
		
		// Face the wall
		float targetYaw = Mathf.Atan2(_cachedWallNormal.X, _cachedWallNormal.Z);
		Player.RigPivot.Rotation = new Vector3(
			Player.RigPivot.Rotation.X,
			targetYaw,
			Player.RigPivot.Rotation.Z
		);
		
		// Set state
		Player.SetState(PlayerState.LedgeGrabbing);
		
		if (DebugManager.IsEnabled(DebugManager.LedgeGrab))
			GD.Print("Grabbed ledge at: ", _ledgePosition);
	}

	private void ReleaseHang()
	{
		_isHanging = false;
		// Explicitly set to Jumping state (player will be airborne after release)
		Player.SetState(PlayerState.Jumping);
		
		if (DebugManager.IsEnabled(DebugManager.LedgeGrab))
			GD.Print("Released from ledge");
	}

	// Public methods for other components
	public bool IsHanging()
	{
		return _isHanging;
	}

	public void ForceRelease()
	{
		if (_isHanging)
			ReleaseHang();
	}

	/// <summary>
	/// Called by Player after MoveAndSlide to maintain hang position
	/// </summary>
	public void ApplyHangPosition()
	{
		if (!_isHanging)
			return;

		// Lock position after MoveAndSlide (use cached wall normal)
		Vector3 hangPosition = _ledgePosition;
		hangPosition += _cachedWallNormal * HangOffsetFromWall;
		hangPosition.Y = _ledgePosition.Y - PlayerReachHeight + HangHeightBelowLedge;
		
		Player.GlobalPosition = hangPosition;
		Player.Velocity = Vector3.Zero;
		
		// Check if grounded (fell off or landed somehow)
		if (Player.IsOnFloor())
		{
			ReleaseHang();
		}
	}

	private void PerformClimb()
	{
		// Give upward velocity only (no horizontal push)
		float climbUpVelocity = Mathf.Sqrt(2.0f * Mathf.Abs(Player.GetPlayerGravity()) * ClimbJumpHeight);
		
		Player.Velocity = new Vector3(0, climbUpVelocity, 0);
		
		// Exit hanging state
		_isHanging = false;
		Player.SetState(PlayerState.Jumping);
		
		if (DebugManager.IsEnabled(DebugManager.LedgeGrab))
			GD.Print("Climbing up from ledge");
	}
}
