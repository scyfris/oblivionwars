using Godot;

[GlobalClass]
public partial class PlayerSaveData : Resource
{
    [Export] public string LastCheckpointId = "";
    [Export] public string LastCheckpointLevelId = "";
    [Export] public float CurrentHealth = 100f;
    [Export] public float MaxHealth = 100f;
    [Export] public int Armor = 0;
    [Export] public int Coins = 0;
    [Export] public string CurrentWeaponId = "";
    [Export] public Godot.Collections.Dictionary<string, bool> UnlockedWeapons = new();
    [Export] public Godot.Collections.Dictionary<string, int> WeaponAmmo = new();
    [Export] public Godot.Collections.Dictionary<int, bool> UnlockedAbilities = new();
    [Export] public Godot.Collections.Dictionary<int, int> Inventory = new();
}
