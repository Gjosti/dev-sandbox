using Godot;

public partial class Attack : Node3D
{
	[Export] public Player Player { get; set; }
	[Export] public float AttackCooldown { get; set; } = 0.2f;
	[Export] public bool AllowAirAttack { get; set; } = true;

	private Timer _timer;
	private AnimationPlayer _animationPlayer;
	private bool _isAttacking = false;
	private SceneTreeTimer _attackTimer;

	public override void _Ready()
	{
		_timer = GetNode<Timer>("Timer");

		if (Player != null)
		{
			_animationPlayer = Player.GetNode<AnimationPlayer>("RigPivot/Rig/CharacterRig/AnimationPlayer");
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("attack"))
		{
			PerformAttack();
		}
	}

	private async void PerformAttack()
	{
		if (!CanAttack()) return;

		_isAttacking = true;
		Player.SetState(PlayerState.Attacking);
		_timer.Start(AttackCooldown);

		// Wait for attack animation to complete
		float attackDuration = GetAttackAnimationDuration();
		_attackTimer = GetTree().CreateTimer(attackDuration);

		await ToSignal(_attackTimer, SceneTreeTimer.SignalName.Timeout);

		FinishAttack();
	}

	private bool CanAttack()
	{
		if (Player == null || _timer == null) return false;

		// Check if player is in a valid state to attack
		bool validGroundState = Player.CurrentState == PlayerState.Idle || Player.CurrentState == PlayerState.Running;
		bool validAirState = AllowAirAttack && Player.CurrentState == PlayerState.Jumping;

		if (!validGroundState && !validAirState)
			return false;

		// Must be off cooldown
		return _timer.IsStopped();
	}

	private float GetAttackAnimationDuration()
	{
		if (_animationPlayer != null && _animationPlayer.HasAnimation("Attack"))
		{
			return (float)_animationPlayer.GetAnimation("Attack").Length;
		}
		return 0.4f; // Fallback to known duration
	}

	private void FinishAttack()
	{
		_isAttacking = false;

		// Only transition if still in attacking state (player might have been interrupted)
		if (Player.CurrentState != PlayerState.Attacking)
			return;

		// Transition back to appropriate movement state
		if (Player.IsOnFloor())
		{
			Vector3 horizontalVel = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);

			if (horizontalVel.Length() > 0.1f || Player.Direction.Length() > 0.1f)
			{
				Player.SetState(PlayerState.Running);
			}
			else
			{
				Player.SetState(PlayerState.Idle);
			}
		}
		else
		{
			// If in air, transition back to jumping
			Player.SetState(PlayerState.Jumping);
		}
	}
}
