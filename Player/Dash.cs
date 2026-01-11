using Godot;
using System.Threading.Tasks;

public partial class Dash : Node3D
{
    [Export] public CharacterBody3D Player { get; set; }
    [Export] public float DashCooldown { get; set; } = 0.5f;
    [Export] public int ExtraDashes { get; set; } = 1;
    [Export] public float DashDuration { get; set; } = 0.5f;
    [Export] public float DashSpeedModifier { get; set; } = 2.0f;
    [Export] public float MinDashSpeed { get; set; } = 6f;
    [Export] public float MaxDashSpeed { get; set; } = 25f;

    private Timer _timer;
    private Rig _rig;
    private Node3D _rigPivot;
    private Vector3 _direction = Vector3.Zero;
    private Vector3 _dashVelocity;
    private int _availableDashes;

    // Called when the node enters the scene tree for the first time.
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
        // Lazy initialize references
        if (_rig == null && Player != null)
        {
            _rig = Player.GetNode<Rig>("RigPivot/Rig");
            _rigPivot = Player.GetNode<Node3D>("RigPivot");
        }

        if (Player != null && Player.IsOnFloor() && _timer.IsStopped())
        {
            _availableDashes = ExtraDashes;
        }
    }

    private async void PerformDash()
    {
        if (_availableDashes <= 0 || !_timer.IsStopped()) return;

        _availableDashes--;
        Vector3 playerDirection = (Vector3)Player.Get("direction");

        _direction = playerDirection.Length() > 0.1f 
            ? playerDirection.Normalized() 
            : -_rigPivot.GlobalTransform.Basis.Z.Normalized();

        Vector3 horizontalVel = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
        float baseDashSpeed = Mathf.Max(horizontalVel.Length(), MinDashSpeed);
        _dashVelocity = _direction * Mathf.Clamp(baseDashSpeed * DashSpeedModifier, MinDashSpeed, MaxDashSpeed);

        if (_direction.Length() > 0.01f)
        {
            _rigPivot.Rotation = new Vector3(_rigPivot.Rotation.X, Mathf.Atan2(-_direction.X, -_direction.Z), _rigPivot.Rotation.Z);
        }

        _rig.Travel("Dash");
        _timer.Start(DashCooldown);

        SceneTreeTimer dashTimer = GetTree().CreateTimer(DashDuration);
        while (dashTimer.TimeLeft > 0)
        {
            Player.Velocity = new Vector3(_dashVelocity.X, Player.Velocity.Y, _dashVelocity.Z);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
