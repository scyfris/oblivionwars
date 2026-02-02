using Godot;

[GlobalClass]
public partial class EnemyDefinition : CharacterDefinition
{
    [ExportGroup("Combat")]
    [Export] public float ContactDamage = 10.0f;
    [Export] public float AggroRange = 200.0f;

    [ExportGroup("Flags")]
    [Export] public bool IsBoss = false;

    [ExportGroup("Drops")]
    [Export] public Godot.Collections.Array<Resource> DropTable;
}
