using Godot;
using System;

[GlobalClass]
public partial class CommonPhysicsDef : Resource
{
    [ExportGroup("Global")]
    [Export]public float Gravity = 2000.0f;

    [ExportGroup("Movement")]
    [Export] public float MoveSpeed = 150.0f;
    [Export] public float JumpStrength = 800.0f;
    [Export] public float WallJumpStrength = 700.0f;
    [Export] public float WallJumpPushAwayForce = 500.0f;
    [Export] public float WallJumpPushAwayDuration = 0.2f;
    [Export] public float WallJumpInputLockDuration = 0.2f;
    [Export] public float WallSlideSpeedFraction = 0.5f;
    [Export] public float DashSpeed = 600.0f;

    [ExportGroup("Knockback")]
    /// <summary>How fast knockback velocity fades per second. Higher = shorter knockback duration.</summary>
    [Export] public float KnockbackDecayRate = 5.0f;
}
