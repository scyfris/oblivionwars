using Godot;

public enum AimMode
{
    TrackPlayer,
    FacingDirection,
}

[GlobalClass]
public partial class AIBehaviorDefinition : Resource
{
    [ExportGroup("Patrol")]
    [Export] public float PatrolRadius = 150f;
    [Export] public float IdlePauseMin = 0.5f;
    [Export] public float IdlePauseMax = 2.0f;

    [ExportGroup("Behavior")]
    [Export] public bool Aggressive = true;
    [Export] public AimMode AimMode = AimMode.TrackPlayer;

    [ExportGroup("Combat")]
    [Export] public float AttackRange = 200f;
    [Export] public float AttackCooldown = 1.0f;
}
