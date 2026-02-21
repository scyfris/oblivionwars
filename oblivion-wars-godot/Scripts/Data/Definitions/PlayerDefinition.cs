using Godot;

[GlobalClass]
public partial class PlayerDefinition : CharacterDefinition
{
    [ExportGroup("Combat")]
    [Export] public float HazardDmgInvincibilityDuration = 1.0f;
}
