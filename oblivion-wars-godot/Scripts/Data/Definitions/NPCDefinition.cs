using Godot;

[GlobalClass]
public partial class NPCDefinition : CharacterDefinition
{
    [ExportGroup("AI")]
    [Export] public NPCAIParameters AIBehaviorData;

    [ExportGroup("Combat")]
    // How much damage is done to the player upon contact.
    [Export] public float ContactDamage = 10.0f;

    [ExportGroup("Flags")]
    [Export] public bool IsBoss = false;

    [ExportGroup("Drops")]
    [Export] public Godot.Collections.Array<DropTableEntry> DropTable = new();
}
