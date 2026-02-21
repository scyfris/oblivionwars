using Godot;
using System.ComponentModel;
using System.Linq;

/// <summary>
/// Global singleton that holds registries for weapons, abilities, items, etc.
/// Configure in the editor by populating arrays with registry entries.
/// </summary>
public partial class GlobalDefinitions : Node
{
    public static GlobalDefinitions Instance { get; private set; }

    // ── Weapon Registry ─────────────────────────────────────
    [ExportGroup("Weapons")]
    [Export] public WeaponRegistryEntry[] Weapons { get; set; } = System.Array.Empty<WeaponRegistryEntry>();
    [Export] public int DefaultWeaponIndex { get; set; } = 0;

    [ExportGroup("Levels")]
    [Export] public HazardDefinition HazardDefs;

    // ── Ability Definitions ────────────────────────────────────
    // TODO: Create AbilityDefinition Resource when needed

    // ── Item Definitions ───────────────────────────────────────
    // TODO: Create ItemDefinition Resource when needed

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

    public WeaponRegistryEntry FindWeaponEntry(string weaponName)
    {
        return System.Array.Find(Weapons, e => e?.Name == weaponName);
    }

    public PackedScene GetWeaponScene(string weaponName)
    {
        return FindWeaponEntry(weaponName)?.Scene;
    }

    public string GetDefaultWeaponName()
    {
        if (DefaultWeaponIndex >= 0 && DefaultWeaponIndex < Weapons.Length)
            return Weapons[DefaultWeaponIndex]?.Name ?? "";
        return "";
    }

    public WeaponRegistryEntry GetWeaponBySlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < Weapons.Length)
            return Weapons[slotIndex];
        return null;
    }

    public string GetWeaponNameBySlot(int slotIndex)
    {
        return GetWeaponBySlot(slotIndex)?.Name;
    }

    public string[] GetAllWeaponNames()
    {
        return Weapons
            .Where(e => e != null)
            .Select(e => e.Name)
            .ToArray();
    }

    // ── Ability Lookup ─────────────────────────────────────────
    // TODO: Implement when AbilityDefinition is created

    // ── Item Lookup ────────────────────────────────────────────
    // TODO: Implement when ItemDefinition is created
}
