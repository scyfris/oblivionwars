using Godot;

[GlobalClass]
public partial class PlayerDefinition : CharacterDefinition
{
    [ExportGroup("Movement")]
    [Export] public float JumpStrength = 800.0f;
    [Export] public float WallJumpStrength = 700.0f;
    [Export] public float DashSpeed = 600.0f;

    [ExportGroup("Physics")]
    [Export] public float Gravity = 2000.0f;
    [Export] public float WallSlideSpeedFraction = 0.5f;
    [Export] public float WallJumpPushAwayForce = 500.0f;
    [Export] public float WallJumpPushAwayDuration = 0.2f;
    [Export] public float WallJumpInputLockDuration = 0.2f;

    [ExportGroup("Combat")]
    [Export] public float InvincibilityDuration = 1.0f;
}
