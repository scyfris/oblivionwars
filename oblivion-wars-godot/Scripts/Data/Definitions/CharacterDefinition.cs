using Godot;


[GlobalClass]
public partial class CharacterDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EntityId = "";


    [ExportGroup("Stats")]
    [Export] public float MaxHealth = 100.0f;
//    [Export] public float KnockbackResistance = 0.0f;

    [ExportGroup("Loadout")]
    [Export] public WeaponRegistryEntry LeftWeapon;
    [Export] public WeaponRegistryEntry RightWeapon;
}
