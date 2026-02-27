using Godot;

[GlobalClass]
public partial class NPCBehaviorParamsAttack_Basic : Resource
{
    [Export] public float AttackDuration = 2.0f;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public AimMode AimMode = AimMode.TrackPlayer;
    [Export] public bool MoveWhileAttacking = false;
    [Export] public bool MoveTowardsPlayerDuringCooldown = true;
    // TODO - aim variance - not sure if should do here or on weapon
}
