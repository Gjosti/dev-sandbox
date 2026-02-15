using Godot;

public partial class Jump : Node3D
{
    [ExportGroup("Jump Settings")]
    [Export] public Player Player { get; set; }
    [Export] public float JumpHeight { get; set; } = 4f;
    [Export] public float JumpTimeToPeak { get; set; } = 0.5f;
    [Export] public float JumpTimeToDescent { get; set; } = 0.25f;
    [Export] public int ExtraJumps { get; set; } = 1;

    [ExportGroup("Crouch/High Jump")]
    [Export] public bool CrouchJumpEnabled { get; set; } = true;
    [Export] public float CrouchJumpHeight { get; set; } = 8f;
    [Export] public float CrouchJumpTimeToPeak { get; set; } = 0.5f;
    [Export] public float CrouchJumpTimeToDescent { get; set; } = 0.25f;

    [ExportGroup("Coyote Time")]
    [Export] public float CoyoteTime { get; set; } = 0.1f;

    [ExportGroup("Jump Cancel")]
    [Export] public float JumpCancelMultiplier { get; set; } = 0.4f;

    [ExportGroup("Bunny Hop")]
    [Export] public bool BunnyHopEnabled { get; set; } = true;
    [Export] public float BunnyHopTimeWindow { get; set; } = 0.2f;
    [Export] public float BunnyHopSpeedThreshold { get; set; } = 9.0f;
    [Export] public float BunnyHopSpeedBoost { get; set; } = 5.0f;

    [ExportGroup("Speed Jump")]
    [Export] public bool SpeedJumpEnabled { get; set; } = true;

    [ExportGroup("VFX")]
    [Export] private GpuParticles3D _jumpVFX;
    [Export] private GpuParticles3D _landVFX;

    // Calculated jump physics
    private float _jumpVelocity;
    private float _jumpGravity;
    private float _fallGravity;
    private float _crouchJumpVelocity;

    // Jump state
    private int _jumpsLeft;
    private float _coyoteTimer;
    private bool _wasOnFloor;
    private float _timeSinceLanding;
    private Rig _rig;

    public override void _Ready()
    {
        InitializeJumpPhysics();
        InitializeReferences();
        
        // Connect to Player's state change signal
        Player.StateChanged += OnPlayerStateChanged;
    }

    private void OnPlayerStateChanged(PlayerState newState, PlayerState oldState)
    {
        // Refresh jumps when transitioning from Jumping to a grounded state
        if (oldState == PlayerState.Jumping && 
            (newState == PlayerState.Idle || newState == PlayerState.Running || newState == PlayerState.Crouching))
        {
            _jumpsLeft = ExtraJumps;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateCoyoteTimer(delta);
        UpdateBunnyHopTimer(delta);
        DetectLanding();
        HandleJumpInput();
        HandleJumpCancel();
    }

    private void InitializeJumpPhysics()
    {
        _jumpVelocity = CalculateJumpVelocity(JumpHeight, JumpTimeToPeak);
        _jumpGravity = CalculateJumpGravity(JumpHeight, JumpTimeToPeak);
        _fallGravity = CalculateFallGravity(JumpHeight, JumpTimeToPeak, JumpTimeToDescent);
        _crouchJumpVelocity = CalculateJumpVelocity(CrouchJumpHeight, CrouchJumpTimeToPeak);
        _jumpsLeft = ExtraJumps;
    }

    private void InitializeReferences()
    {
        _rig = Player.GetNode<Rig>("RigPivot/Rig");
        _wasOnFloor = Player.IsOnFloor();
    }

    private float CalculateJumpVelocity(float height, float timeToPeak)
    {
        return (2.0f * height) / timeToPeak;
    }

    private float CalculateJumpGravity(float height, float timeToPeak)
    {
        return (-2.0f * height) / (timeToPeak * timeToPeak);
    }

    private float CalculateFallGravity(float height, float timeToPeak, float timeToDescent)
    {
        return (-2.0f * height) / (timeToPeak * timeToDescent);
    }

    private void UpdateCoyoteTimer(double delta)
    {
        if (Player.IsOnFloor())
        {
            _coyoteTimer = CoyoteTime;
        }
        else
        {
            _coyoteTimer = Mathf.Max(0.0f, _coyoteTimer - (float)delta);
        }
    }

    private void UpdateBunnyHopTimer(double delta)
    {
        if (Player.IsOnFloor())
        {
            _timeSinceLanding += (float)delta;
        }
        else
        {
            _timeSinceLanding = 0.0f;
        }
    }

    private void DetectLanding()
    {
        bool isOnFloor = Player.IsOnFloor();
        
        if (isOnFloor && !_wasOnFloor)
        {
            _landVFX?.Restart();
            _timeSinceLanding = 0.0f;
        }
        
        _wasOnFloor = isOnFloor;
    }

    private void HandleJumpInput()
    {
        if (Input.IsActionJustPressed("jump"))
        {
            if (CrouchJumpEnabled && Player.CurrentState == PlayerState.Crouching)
            {
                PerformCrouchJump();
            }
            else
            {
                PerformJump();
            }
        }
    }

    private void HandleJumpCancel()
    {
        if (Input.IsActionJustReleased("jump") && Player.Velocity.Y > 0)
        {
            Player.Velocity = new Vector3(
                Player.Velocity.X, 
                Player.Velocity.Y * JumpCancelMultiplier, 
                Player.Velocity.Z
            );
        }
    }

    private void PerformJump()
    {
        if (CanGroundJump())
        {
            bool applyBunnyHop = ShouldApplyBunnyHop();
            ExecuteGroundJump(_jumpVelocity, "Jump", applyBunnyHop);
        }
        else if (CanAirJump())
        {
            ExecuteAirJump(_jumpVelocity, "Jump");
        }
    }

    private void PerformCrouchJump()
    {
        if (CanGroundJump())
        {
            ExecuteGroundJump(_crouchJumpVelocity, "CrouchJump", false);
        }
        else if (CanAirJump())
        {
            // Prevent high jump in air - use regular jump velocity
            ExecuteAirJump(_jumpVelocity, "Jump");
        }
    }

    private bool CanGroundJump()
    {
        return Player.IsOnFloor() || _coyoteTimer > 0.0f;
    }

    private bool CanAirJump()
    {
        return _jumpsLeft > 0;
    }

    private bool ShouldApplyBunnyHop()
    {
        if (!BunnyHopEnabled) return false;

        Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
        float currentSpeed = horizontalVelocity.Length();

        return _timeSinceLanding <= BunnyHopTimeWindow && currentSpeed >= BunnyHopSpeedThreshold;
    }

    private void ExecuteGroundJump(float jumpVelocity, string animationName, bool applyBunnyHop = false)
    {
        Player.Velocity = new Vector3(Player.Velocity.X, Player.Velocity.Y + jumpVelocity, Player.Velocity.Z);
        
        if (applyBunnyHop)
        {
            ApplyBunnyHopBoost();
        }
        
        _coyoteTimer = 0.0f;
        PlayJumpEffects(animationName);
    }

    private void ApplyBunnyHopBoost()
    {
        Vector3 horizontalVelocity = new Vector3(Player.Velocity.X, 0, Player.Velocity.Z);
        Vector3 boostedVelocity = horizontalVelocity.Normalized() * (horizontalVelocity.Length() + BunnyHopSpeedBoost);
        Player.Velocity = new Vector3(boostedVelocity.X, Player.Velocity.Y, boostedVelocity.Z);
    }

    private void ExecuteAirJump(float jumpVelocity, string animationName)
    {
        _jumpsLeft--;
        Player.Velocity = new Vector3(Player.Velocity.X, jumpVelocity, Player.Velocity.Z);
        PlayJumpEffects(animationName);
    }

    private void PlayJumpEffects(string animationName)
    {
        Player.SetState(PlayerState.Jumping);
        _jumpVFX?.Restart();
    }

    public float GetGravity(float currentVelocityY)
    {
        return currentVelocityY > 0.0f ? _jumpGravity : _fallGravity;
    }
}
