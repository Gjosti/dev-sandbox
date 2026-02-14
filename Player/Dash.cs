using Godot;

public partial class Dash : Node3D
{
    [Export] public Player Player { get; set; }
    [Export] public float DashCooldown { get; set; } = 0.5f;
    [Export] public int ExtraDashes { get; set; } = 1;
    [Export] public float DashSpeed { get; set; } = 20f;

    private Timer _timer;
    private Rig _rig;
    private Node3D _rigPivot;
    private AnimationPlayer _animationPlayer;
    private int _availableDashes;

    public override void _Ready()
    {
        _timer = GetNode<Timer>("Timer");
        _availableDashes = ExtraDashes;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("dash"))
        {
            PerformDash();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        InitializeReferences();
        RefreshDashesOnGround();
    }

    private void InitializeReferences()
    {
        if (_rig == null && Player != null)
        {
            _rig = Player.GetNode<Rig>("RigPivot/Rig");
            _rigPivot = Player.GetNode<Node3D>("RigPivot");
            _animationPlayer = Player.GetNode<AnimationPlayer>("RigPivot/Rig/CharacterRig/AnimationPlayer");
        }
    }

    private void RefreshDashesOnGround()
    {
        if (Player != null && Player.IsOnFloor() && _timer.IsStopped())
        {
            _availableDashes = ExtraDashes;
        }
    }

    private async void PerformDash()
    {
        if (!CanDash()) return;

        _availableDashes--;
        
        Vector3 dashDirection = CalculateDashDirection();
        Vector3 dashVelocity = dashDirection * DashSpeed;
        
        RotatePlayerToDashDirection(dashDirection);
        StartDashAnimation();
        
        await ApplyDashMovement(dashVelocity);
    }

    private bool CanDash()
    {
        return _availableDashes > 0 && _timer.IsStopped() && !_rig.IsCrouching();
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

    private void StartDashAnimation()
    {
        _rig.Travel("Dash");
        _timer.Start(DashCooldown);
    }

    private async System.Threading.Tasks.Task ApplyDashMovement(Vector3 dashVelocity)
    {
        float dashDuration = (float)_animationPlayer.GetAnimation("Dash").Length;
        SceneTreeTimer dashTimer = GetTree().CreateTimer(dashDuration);
        
        while (dashTimer.TimeLeft > 0)
        {
            Vector3 currentHorizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
            
            // Only apply dash velocity if it's faster than current velocity
            Vector3 finalVelocity = currentHorizontalVelocity.Length() > dashVelocity.Length() 
                ? currentHorizontalVelocity 
                : dashVelocity;
            
            Player.Velocity = new Vector3(finalVelocity.X, Player.Velocity.Y, finalVelocity.Z);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
