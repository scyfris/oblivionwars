using Godot;

[GlobalClass]
public partial class StatusEffectDefinition : Resource
{
    // unique id
    [Export] public string EffectId = "";

    // in-game display string
    [Export] public string DisplayName = "";

    // how long it lasts before status wears off
    [Export] public float DefaultDuration = 3.0f;

    // Can multiple status effects be applied?
    [Export] public bool Stackable = false;

    // If stackable, how many stacks can be applied?
    [Export] public int MaxStacks = 1;

    // If non-0, specifies time between damage  (secs)
    [Export] public float TickInterval = 0.0f;

    // If TickInterval non-0, how much damage each tick time (secs)
    [Export] public float TickDamage = 0.0f;

    // If non-1, modifies speed
    [Export] public float SpeedMultiplier = 1.0f;
    
    // If non-1, modifies weapon damage.
    [Export] public float DamageMultiplier = 1.0f;
}
