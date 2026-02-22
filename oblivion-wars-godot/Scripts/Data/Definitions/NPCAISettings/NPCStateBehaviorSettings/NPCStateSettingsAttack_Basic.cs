using Godot;

[GlobalClass]
public partial class NPCStateSettingsAttack_Basic : Resource
{
    [Export] public float AttackCooldown = 1.0f;
    [Export] public AimMode AimMode = AimMode.TrackPlayer;
}
