using Godot;

public partial class Dash : Node3D
{
    [Export] public Player Player { get; set; }
    [Export] public float DashCooldown { get; set; } = 0.5f;
    [Export] public int ExtraDashes { get; set; } = 1;
    [Export] public float DashSpeed { get; set; } = 20f;
    [Export] public float DirectionChangeThreshold { get; set; } = 0.7f; // ~45 degrees

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
        float dashDuration = GetDashAnimationDuration();
        SceneTreeTimer dashTimer = GetTree().CreateTimer(dashDuration);
        
        while (dashTimer.TimeLeft > 0)
        {
            Vector3 finalVelocity = CalculateDashVelocity(dashVelocity);
            Player.Velocity = new Vector3(finalVelocity.X, Player.Velocity.Y, finalVelocity.Z);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private float GetDashAnimationDuration()
    {
        return (float)_animationPlayer.GetAnimation("Dash").Length;
    }

    private Vector3 CalculateDashVelocity(Vector3 dashVelocity)
    {
        Vector3 currentHorizontalVelocity = GetHorizontalVelocity();
        
        if (!ShouldCheckDirection(currentHorizontalVelocity, dashVelocity))
        {
            return dashVelocity;
        }

        float directionSimilarity = GetDirectionSimilarity(currentHorizontalVelocity, dashVelocity);
        
        return IsSignificantDirectionChange(directionSimilarity)
            ? dashVelocity
            : SnapToDashDirectionWithHigherSpeed(dashVelocity, currentHorizontalVelocity);
    }

    private Vector3 GetHorizontalVelocity()
    {
        return new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
    }

    private bool ShouldCheckDirection(Vector3 currentVelocity, Vector3 dashVelocity)
    {
        return currentVelocity.Length() > 0.1f && dashVelocity.Length() > 0.1f;
    }

    private float GetDirectionSimilarity(Vector3 currentVelocity, Vector3 dashVelocity)
    {
        return currentVelocity.Normalized().Dot(dashVelocity.Normalized());
    }

    private bool IsSignificantDirectionChange(float directionSimilarity)
    {
        return directionSimilarity < DirectionChangeThreshold;
    }

    private Vector3 SnapToDashDirectionWithHigherSpeed(Vector3 dashVelocity, Vector3 currentVelocity)
    {
        float currentSpeed = currentVelocity.Length();
        float finalSpeed = Mathf.Max(currentSpeed, dashVelocity.Length());
        return dashVelocity.Normalized() * finalSpeed;
    }
}
