using Godot;

[GlobalClass]
public partial class PlayerDefinition : CharacterDefinition
{
    [ExportGroup("Movement")]
    [Export] public float DashSpeed = 600.0f;

    [ExportGroup("Combat")]
    [Export] public float InvincibilityDuration = 1.0f;
}
