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
    // Range which the NPC detects the player
    [Export] public float DetectionRange = 400.0f;
    // The range at which the NPC can attack the player (otherwise they will have to move closer)
    [Export] public float AttackRange = 200.0f;

    [ExportGroup("Behavior")]
    // Whether to attack player first.  True means will attack player if detected.  False means won't attack player unless shot first.
    [Export] public bool Aggressive = true;

    // When the NPC starts to flee from the player out of detection range.
    // Set to 0 for no flee threshold, otherwise it is a fractional amount (1 being will always flee)
    [Export(PropertyHint.Range, "0,1,0.05")] public float FleeHealthThreshold = 0.25f;
}
