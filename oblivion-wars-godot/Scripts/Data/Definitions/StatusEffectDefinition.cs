using Godot;

[GlobalClass]
public partial class StatusEffectDefinition : Resource
{
    [Export] public string EffectId = "";
    [Export] public string DisplayName = "";
    [Export] public float DefaultDuration = 3.0f;
    [Export] public bool Stackable = false;
    [Export] public int MaxStacks = 1;
    [Export] public float TickInterval = 0.0f;
    [Export] public float TickDamage = 0.0f;
    [Export] public float SpeedMultiplier = 1.0f;
    [Export] public float DamageMultiplier = 1.0f;
}
