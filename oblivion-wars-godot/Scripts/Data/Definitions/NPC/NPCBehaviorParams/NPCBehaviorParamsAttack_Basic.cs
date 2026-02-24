using Godot;

[GlobalClass]
public partial class NPCBehaviorParamsAttack_Basic : Resource
{
    [Export] public float AttackCooldown = 1.0f;
    [Export] public AimMode AimMode = AimMode.TrackPlayer;
    // TODO - aim variance - not sure if should do here or on weapon
}
