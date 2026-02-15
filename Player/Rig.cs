using Godot;

public partial class Rig : Node3D
{
	[Export] public float AnimationSpeed { get; set; } = 10.0f;
	[Export] public bool DebugMode { get; set; } = true;

	private AnimationTree _animationTree;
	private AnimationNodeStateMachinePlayback _playback;
	private Label3D _stateLabel;

	private const string RunPath = "parameters/MoveSpace/blend_position";
	private float _runWeightTarget = -1.0f;
	private PlayerState _currentGameState = PlayerState.Idle;

	public override void _Ready()
	{
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_playback = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
		_stateLabel = GetNode<Label3D>("StateLabel");
	}

	public override void _PhysicsProcess(double delta)
	{
		// Smoothly blend run animation weight
		float currentValue = (float)_animationTree.Get(RunPath);
		_animationTree.Set(RunPath, Mathf.MoveToward(
			currentValue,
			_runWeightTarget,
			(float)delta * AnimationSpeed
		));

		if (DebugMode)
		{
			// Show both animation state and game state for debugging
			_stateLabel.Text = $"Game: {_currentGameState}\nAnim: {_playback.GetCurrentNode()}";
		}
	}

	/// <summary>
	/// Updates animations based on game state. This is called from Player._PhysicsProcess.
	/// The Rig reads the game state and drives animations accordingly.
	/// </summary>
	public void UpdateFromGameState(PlayerState gameState, Vector3 direction)
	{
		_currentGameState = gameState;

		// Update movement blend space
		UpdateMovementBlend(direction);

		// Drive animation state machine based on game state
		DriveAnimationFromGameState(gameState);
	}

	private void UpdateMovementBlend(Vector3 direction)
	{
		if (direction.IsZeroApprox())
		{
			_runWeightTarget = -1.0f;
		}
		else
		{
			_runWeightTarget = 1.0f;
		}
	}

	private void DriveAnimationFromGameState(PlayerState gameState)
	{
		// Map game states to animation state machine nodes
		string targetAnimation = gameState switch
		{
			PlayerState.Idle => "MoveSpace",
			PlayerState.Running => "MoveSpace",
			PlayerState.Jumping => "Jump",
			PlayerState.Dashing => "Dash",
			PlayerState.Crouching => "Crouch",
			PlayerState.Sliding => "Slide",
			PlayerState.LedgeGrabbing => "LedgeGrab",
			PlayerState.Attacking => "Attack",
			_ => "MoveSpace"
		};

		// Travel to animation if we're not already there
		if (_playback.GetCurrentNode() != targetAnimation)
		{
			_playback.Travel(targetAnimation);
		}
	}

	/// <summary>
	/// Legacy method for manual animation control. 
	/// Prefer using Player.SetState() instead for better separation of concerns.
	/// </summary>
	public void Travel(string animationName)
	{
		_playback.Travel(animationName);
	}

	// ============================================================
	// Legacy State Query Methods (kept for backward compatibility)
	// These now check the AnimationTree state, not game logic state.
	// Prefer checking Player.CurrentState directly instead.
	// ============================================================
	
	public bool IsIdle()
	{
		return _playback.GetCurrentNode() == "MoveSpace";
	}

	public bool IsDashing()
	{
		return _playback.GetCurrentNode() == "Dash";
	}

	public bool IsJumping()
	{
		return _playback.GetCurrentNode() == "Jump";
	}

	public bool IsAttacking()
	{
		return _playback.GetCurrentNode() == "Attack";
	}

	public bool IsCrouching()
	{
		return _playback.GetCurrentNode() == "Crouch";
	}

	public bool IsSliding()
	{
		return _playback.GetCurrentNode() == "Slide";
	}

	public bool IsLedgeGrabbing()
	{
		return _playback.GetCurrentNode() == "LedgeGrab";
	}
}
