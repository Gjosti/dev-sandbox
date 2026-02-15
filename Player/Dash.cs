using Godot;

public partial class Dash : Node3D
{
    [Export] public Player Player { get; set; }
    [Export] public float DashCooldown { get; set; } = 0.5f;
    [Export] public int ExtraDashes { get; set; } = 1;
    [Export] public float DashSpeed { get; set; } = 20f;
    [Export] public float DirectionChangeThreshold { get; set; } = 0.7f; // ~45 degrees
    [Export] public float DashRefreshDelay { get; set; } = 1f;

    private Timer _dashCooldownTimer;
    private Timer _dashRefreshTimer;
    private Rig _rig;
    private Node3D _rigPivot;
    private AnimationPlayer _animationPlayer;
    private int _availableDashes;

    public override void _Ready()
    {
        _dashCooldownTimer = GetNode<Timer>("Timer");
        _dashRefreshTimer = _dashCooldownTimer; // Reuse the same timer
        _availableDashes = ExtraDashes;
        
        if (Player != null)
        {
            _rigPivot = Player.GetNode<Node3D>("RigPivot");
            _animationPlayer = Player.GetNode<AnimationPlayer>("RigPivot/Rig/CharacterRig/AnimationPlayer");
        }
        
        _dashCooldownTimer.Timeout += RefreshDashes;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("dash"))
        {
            PerformDash();
        }
    }

    private async void PerformDash()
    {
        if (_availableDashes <= 0 || !_dashCooldownTimer.IsStopped() || 
            Player.CurrentState == PlayerState.Crouching || 
            Player.CurrentState == PlayerState.LedgeGrabbing)
            return;

        _availableDashes--;
        _dashCooldownTimer.Start(DashCooldown);
        
        Vector3 dashDirection = CalculateDashDirection();
        RotatePlayerToDashDirection(dashDirection);
        Player.SetState(PlayerState.Dashing);
        
        await ApplyDashMovement(dashDirection * DashSpeed);
        
        // Return to idle or running based on current velocity
        Vector3 horizontalVel = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
        Player.SetState(horizontalVel.Length() > 0.1f ? PlayerState.Running : PlayerState.Idle);
    }

    private Vector3 CalculateDashDirection()
    {
        Vector3 playerDirection = (Vector3)Player.Direction;
        return playerDirection.Length() > 0.1f 
            ? playerDirection.Normalized() 
            : -_rigPivot.GlobalTransform.Basis.Z.Normalized();
    }

    private void RotatePlayerToDashDirection(Vector3 direction)
    {
        if (direction.Length() > 0.01f)
        {
            float targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
            _rigPivot.Rotation = new Vector3(_rigPivot.Rotation.X, targetYaw, _rigPivot.Rotation.Z);
        }
    }

    private async System.Threading.Tasks.Task ApplyDashMovement(Vector3 dashVelocity)
    {
        float dashDuration = (float)_animationPlayer.GetAnimation("Dash").Length;
        SceneTreeTimer dashTimer = GetTree().CreateTimer(dashDuration);
        
        while (dashTimer.TimeLeft > 0)
        {
            Player.Velocity = ApplyDashVelocity(dashVelocity);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private Vector3 ApplyDashVelocity(Vector3 dashVelocity)
    {
        Vector3 currentHorizontal = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
        
        // If no existing velocity or significant direction change, use pure dash velocity
        if (currentHorizontal.Length() <= 0.1f || 
            currentHorizontal.Normalized().Dot(dashVelocity.Normalized()) < DirectionChangeThreshold)
        {
            return new Vector3(dashVelocity.X, Player.Velocity.Y, dashVelocity.Z);
        }
        
        // Otherwise, keep faster of the two speeds
        float finalSpeed = Mathf.Max(currentHorizontal.Length(), dashVelocity.Length());
        return new Vector3(
            dashVelocity.Normalized().X * finalSpeed, 
            Player.Velocity.Y, 
            dashVelocity.Normalized().Z * finalSpeed
        );
    }

    private void RefreshDashes()
    {
        _availableDashes = ExtraDashes;
    }
}
