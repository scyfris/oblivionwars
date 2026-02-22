using Godot;

// These are global and help the NPCController select which state the NPC should be in.
public enum AimMode
{
    TrackPlayer,
    FacingDirection,
}

[GlobalClass]
// Controls things like when to transition states and such
public partial class NPCAIParameters : Resource
{
    //
    // All of htese are for specifying when states transition
    //

    [ExportGroup("Ranges")]
    // When to leave idle/patrol
    [Export] public float DetectionRange = 400.0f;
    // When to enter attack state
    [Export] public float AggroRange = 200.0f;

    [ExportGroup("Patrol")]

    [Export] public float PatrolRadius = 150f;// XXX - move to NPCStateSettingsPatrol_Basic
    [Export] public float IdlePauseMin = 0.5f;// XXX - Move to NPCStateSettingsIdle_Basic
    [Export] public float IdlePauseMax = 2.0f;// XXX - Move to NPCStateSettingsIdle_Basic

    [ExportGroup("Behavior")]
    // Whether to attack player. 
    [Export] public bool Aggressive = true;

    [Export] public AimMode AimMode = AimMode.TrackPlayer; 
    [Export(PropertyHint.Range, "0,1,0.05")] public float FleeHealthThreshold = 0.25f;

    // XXX Remove this!
    [ExportGroup("Combat")]
    [Export] public float AttackRange = 200f; // XXX - Move to NPCStateSettingsAttack_Basic
}
