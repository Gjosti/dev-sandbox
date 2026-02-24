using Godot;

public partial class PlayerStateMachine : Node
{
	[Export] public Player Player { get; set; }

	public override void _Ready()
	{
		if (Player == null)
		{
			Player = GetParent<Player>();
		}
	}

	public void EvaluateAndUpdateState()
	{
		if (Player == null)
			return;

		if (Player.CurrentState == PlayerState.Dashing
		    || Player.CurrentState == PlayerState.Attacking
		    || Player.CurrentState == PlayerState.LedgeGrabbing)
		{
			return;
		}

		if (Player.CurrentState == PlayerState.Crouching || Player.CurrentState == PlayerState.Sliding)
		{
			return;
		}

		if (!Player.IsOnFloor())
		{
			if (Player.CurrentState != PlayerState.Jumping)
			{
				Player.SetState(PlayerState.Jumping);
			}
		}
		else
		{
			UpdateGroundedState();
		}
	}

	private void UpdateGroundedState()
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
}
