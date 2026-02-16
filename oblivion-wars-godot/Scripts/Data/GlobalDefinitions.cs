using Godot;

/// <summary>
/// Global singleton that holds mappings from enums to definition Resources.
/// Configure these in the editor by assigning .tres definition files.
/// </summary>
public partial class GlobalDefinitions : Node
{
    public static GlobalDefinitions Instance { get; private set; }

    // ── Weapon Definitions ─────────────────────────────────────
    [ExportGroup("Weapons")]
    [Export] public WeaponDefinition PistolDefinition { get; set; }
    [Export] public WeaponDefinition ShotgunDefinition { get; set; }
    [Export] public WeaponDefinition MachineGunDefinition { get; set; }
    [Export] public WeaponDefinition RocketLauncherDefinition { get; set; }
    [Export] public WeaponDefinition GrenadeLauncherDefinition { get; set; }
    [Export] public WeaponDefinition LaserDefinition { get; set; }
    [Export] public WeaponDefinition RailgunDefinition { get; set; }

    // ── Ability Definitions ────────────────────────────────────
    // TODO: Create AbilityDefinition Resource when needed
    // [ExportGroup("Abilities")]
    // [Export] public AbilityDefinition DoubleJumpDefinition { get; set; }
    // etc.

    // ── Item Definitions ───────────────────────────────────────
    // TODO: Create ItemDefinition Resource when needed
    // [ExportGroup("Items")]
    // [Export] public ItemDefinition HealthPackDefinition { get; set; }
    // etc.

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("GlobalDefinitions: Duplicate instance detected!");
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── Weapon Lookup ──────────────────────────────────────────

    public WeaponDefinition GetWeaponDefinition(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Pistol => PistolDefinition,
            WeaponType.Shotgun => ShotgunDefinition,
            WeaponType.MachineGun => MachineGunDefinition,
            WeaponType.RocketLauncher => RocketLauncherDefinition,
            WeaponType.GrenadeLauncher => GrenadeLauncherDefinition,
            WeaponType.Laser => LaserDefinition,
            WeaponType.Railgun => RailgunDefinition,
            _ => null
        };
    }

    public string GetWeaponName(WeaponType weaponType)
    {
        var def = GetWeaponDefinition(weaponType);
        return def?.WeaponId ?? weaponType.ToString();
    }

    // ── Ability Lookup ─────────────────────────────────────────
    // TODO: Implement when AbilityDefinition is created

    // ── Item Lookup ────────────────────────────────────────────
    // TODO: Implement when ItemDefinition is created
}
