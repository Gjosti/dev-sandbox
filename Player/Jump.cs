using Godot;

public partial class Jump : Node3D
{
    [ExportGroup("Jump Settings")]
    [Export] public Player Player { get; set; } 
    [Export] public float JumpHeight { get; set; } = 4f;
    [Export] public float JumpTimeToPeak { get; set; } = 0.5f;
    [Export] public float JumpTimeToDescent { get; set; } = 0.25f;
    [Export] public int ExtraJumps { get; set; } = 1;

    [ExportGroup("Crouch/High Jump (Crouch + Jump while still)")]
    [Export] public bool CrouchJumpEnabled { get; set; } = true;
    [Export] public float CrouchJumpHeight { get; set; } = 8f;
    [Export] public float CrouchJumpTimeToPeak { get; set; } = 0.5f;
    [Export] public float CrouchJumpTimeToDescent { get; set; } = 0.25f;

    [ExportGroup("Speed Jump")]
    [Export] public bool SpeedJumpEnabled { get; set; } = true; // TODO: Jumping from the ground while above a certain horizontal velocity threshold adds a slight velocity boost

    // Regular Jump - calculated values
    private float _jumpVelocity;
    private float _jumpGravity;
    private float _fallGravity;

    // Crouch/High Jump - calculated values
    private float _crouchJumpVelocity;

    private int _jumpsLeft;

    private const float CoyoteTime = 0.1f;
    private float _coyoteTimer = 0.0f;

    private Rig _rig;
    private GpuParticles3D _jumpVFX;

    public override void _Ready()
    {
        // Calculate jump physics values
        _jumpVelocity = (2.0f * JumpHeight) / JumpTimeToPeak;
        _jumpGravity = (-2.0f * JumpHeight) / (JumpTimeToPeak * JumpTimeToPeak);
        _fallGravity = (-2.0f * JumpHeight) / (JumpTimeToPeak * JumpTimeToDescent);

        _crouchJumpVelocity = (2.0f * CrouchJumpHeight) / CrouchJumpTimeToPeak;

        _jumpsLeft = ExtraJumps;

		_rig = Player.GetNode<Rig>("RigPivot/Rig");
        _jumpVFX = GetNode<GpuParticles3D>("VFX/GPUParticles3D"); //If multiple FX change to animationplayer
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Player.IsOnFloor())
        {
            _jumpsLeft = ExtraJumps;
        }

        HandleJumpInput();

        // Coyote time
        if (Player.IsOnFloor())
        {
            _coyoteTimer = CoyoteTime;
        }
        else
        {
            _coyoteTimer = Mathf.Max(0.0f, _coyoteTimer - (float)delta);
        }

        // Cancel jump early
        if (Input.IsActionJustReleased("jump") && Player.Velocity.Y >= 0)
        {
            Player.Velocity = new Vector3(Player.Velocity.X, Player.Velocity.Y * 0.4f, Player.Velocity.Z);
        }
    }

    private void HandleJumpInput()
    {
        if (Input.IsActionJustPressed("jump"))
        {
            if (CrouchJumpEnabled && _rig.IsCrouching())
            {
                CrouchJump();
            }
            else
            {
                PerformJump();
            }
        }
    }

    private void PerformJump()
    {
        if (Player.IsOnFloor() || _coyoteTimer > 0.0f)
        {
            Player.Velocity = new Vector3(Player.Velocity.X, Player.Velocity.Y + _jumpVelocity, Player.Velocity.Z);
            _coyoteTimer = 0.0f;
            _rig.Travel("Jump");
            _jumpVFX.Restart();
        }
        else if (_jumpsLeft > 0)
        {
            _jumpsLeft--;
            Player.Velocity = new Vector3(Player.Velocity.X, _jumpVelocity, Player.Velocity.Z);
            _rig.Travel("Jump");
            _jumpVFX.Restart();
        }
    }

    private void CrouchJump()
    {
        if (Player.IsOnFloor() || _coyoteTimer > 0.0f)
        {
            Player.Velocity = new Vector3(Player.Velocity.X, Player.Velocity.Y + _crouchJumpVelocity, Player.Velocity.Z);
            _coyoteTimer = 0.0f;
            _rig.Travel("CrouchJump");
            _jumpVFX.Restart();
        }
        else if (_jumpsLeft > 0)
        {
            _jumpsLeft--;
            Player.Velocity = new Vector3(Player.Velocity.X, _jumpVelocity, Player.Velocity.Z); // So that the player cannot high jump in air
            _rig.Travel("Jump");
            _jumpVFX.Restart();
        }
    }

    // TODO: Recheck if needed
    public float GetGravity(float currentVelocityY)
    {
        return currentVelocityY > 0.0f ? _jumpGravity : _fallGravity;
    }
}
