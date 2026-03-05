using Godot;


[GlobalClass]
public partial class CharacterDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EntityId = "";


    [ExportGroup("Stats")]
    [Export] public float MaxHealth = 100.0f;
    /// <summary>0 = full knockback, 1 = immune. Scales incoming impact force by (1 - resistance).</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float KnockbackResistance = 0.0f;

    [ExportGroup("Loadout")]
    [Export] public WeaponRegistryEntry LeftWeapon;
    [Export] public WeaponRegistryEntry RightWeapon;
}
