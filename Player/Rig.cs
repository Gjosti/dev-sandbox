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

	public override void _Ready()
	{
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_playback = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
		_stateLabel = GetNode<Label3D>("StateLabel");
	}



	public override void _PhysicsProcess(double delta)
	{
		float currentValue = (float)_animationTree.Get(RunPath);
		_animationTree.Set(RunPath, Mathf.MoveToward(
			currentValue,
			_runWeightTarget,
			(float)delta * AnimationSpeed
		));

		if (DebugMode)
		{
			_stateLabel.Text = _playback.GetCurrentNode();
		}
	}

	public void UpdateAnimationTree(Vector3 direction)
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

	public void Travel(string animationName)
	{
		_playback.Travel(animationName);
	}

	// Rig States
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
