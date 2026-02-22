using Godot;

// These are global and help the NPCController select which state the NPC should be in.
public enum AimMode
{
    TrackPlayer,
    FacingDirection,
}

[GlobalClass]
public partial class NPCBehaviorSettingsGlobal : Resource
{
    [ExportGroup("Ranges")]
    [Export] public float DetectionRange = 400.0f;
    [Export] public float AggroRange = 200.0f;

    // XXX Remove this!
    [ExportGroup("Patrol")]
    [Export] public float PatrolRadius = 150f;
    [Export] public float IdlePauseMin = 0.5f;
    [Export] public float IdlePauseMax = 2.0f;

    [ExportGroup("Behavior")]
    [Export] public bool Aggressive = true;
    [Export] public AimMode AimMode = AimMode.TrackPlayer;
    [Export(PropertyHint.Range, "0,1,0.05")] public float FleeHealthThreshold = 0.25f;

    // XXX Remove this!
    [ExportGroup("Combat")]
    [Export] public float AttackRange = 200f;
    [Export] public float AttackCooldown = 1.0f;
}
