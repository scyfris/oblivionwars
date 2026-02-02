using Godot;

public enum PersistenceMode
{
    None,
    FlagsOnly,
    Full
}

[GlobalClass]
public partial class CharacterDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EntityId = "";

    [ExportGroup("Stats")]
    [Export] public float MaxHealth = 100.0f;
    [Export] public float MoveSpeed = 300.0f;
    [Export] public float KnockbackResistance = 0.0f;
    [Export] public float Mass = 1.0f;

    [ExportGroup("Persistence")]
    [Export] public PersistenceMode Persistence = PersistenceMode.None;
}
