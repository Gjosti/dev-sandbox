using Godot;

public partial class Player : CharacterBody3D
{
    [Signal]
    public delegate void VelocityCurrentEventHandler(Vector3 currentVelocity);

    [ExportGroup("Movement Settings")]
    [Export] public float MovementSpeed { get; set; } = 6.0f;
    [Export] public float CrouchMovementModifier { get; set; }
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

    public Vector3 Direction { get; set; } = Vector3.Zero;
    private Vector3 _horizontalVelocity = Vector3.Zero;

    public Node3D PlayerMesh { get; private set; }
    public CollisionShape3D CollisionShape { get; private set; }
    public Node3D RigPivot { get; private set; }
    public Rig Rig { get; private set; }
    private Jump _jump;
    private PlayerCamera _camera;

    public override void _Ready()
    {
        PlayerMesh = GetNode<Node3D>("RigPivot/Rig/CharacterRig/MeshInstance3D");
        CollisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        RigPivot = GetNode<Node3D>("RigPivot");
        Rig = GetNode<Rig>("RigPivot/Rig");
        _jump = GetNode<Jump>("Jump");
        _camera = GetNode<PlayerCamera>("PlayerCamera");
    }

    public override void _PhysicsProcess(double delta)
    {
        ApplyGravity((float)delta);
        HandleMovement((float)delta);
        MoveAndSlide();

        EmitSignal(SignalName.VelocityCurrent, Velocity);
    }

    private void ApplyGravity(float delta)
    {
        if (Rig.IsDashing())
        {
            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
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
        if (Rig.IsDashing() || Rig.IsSliding())
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
        Rig.UpdateAnimationTree(Direction);
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

    public override void _UnhandledInput(InputEvent @event)
    {
        HandleAttackInput(@event);
    }

    private void HandleAttackInput(InputEvent @event)
    {
        if (Rig.IsIdle())
        {
            if (@event.IsActionPressed("attack"))
            {
                MainAction();
            }
        }
    }

    private void MainAction()
    {
        Rig.Travel("Attack");
    }
}
