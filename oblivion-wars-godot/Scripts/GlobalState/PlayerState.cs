using Godot;
using System.Linq;

/// <summary>
/// Holds all player-specific persistent data.
/// Accessed via GlobalStateManager.Instance.Player
/// </summary>
public class PlayerState : ISaveableState<PlayerSaveData>
{
    // Health & Stats
    public float CurrentHealth = 100f;
    public float MaxHealth = 100f;
    public int Armor = 0;
    public int Coins = 0;

    // Checkpoint tracking
    public string LastCheckpointId = "";
    public string LastCheckpointLevelId = "";

    // Weapons (WeaponType -> unlocked)
    private Godot.Collections.Dictionary<int, bool> _unlockedWeapons = new();

    // Ammo (WeaponType -> ammo count, -1 = infinite)
    private Godot.Collections.Dictionary<int, int> _weaponAmmo = new();

    // Abilities (AbilityType -> unlocked)
    private Godot.Collections.Dictionary<int, bool> _unlockedAbilities = new();

    // Inventory items (ItemType -> quantity)
    private Godot.Collections.Dictionary<int, int> _inventory = new();

    // ── Weapon Management ──────────────────────────────────────

    public void UnlockWeapon(WeaponType weaponType, int startingAmmo = 0)
    {
        int key = (int)weaponType;
        if (!_unlockedWeapons.ContainsKey(key))
        {
            _unlockedWeapons[key] = false;
        }

        if (!_unlockedWeapons[key])
        {
            _unlockedWeapons[key] = true;
            _weaponAmmo[key] = startingAmmo;
            GD.Print($"Weapon unlocked: {weaponType}");
        }
    }

    public bool IsWeaponUnlocked(WeaponType weaponType)
    {
        int key = (int)weaponType;
        return _unlockedWeapons.ContainsKey(key) && _unlockedWeapons[key];
    }

    public void AddAmmo(WeaponType weaponType, int amount)
    {
        if (!IsWeaponUnlocked(weaponType))
            return;

        int key = (int)weaponType;
        if (!_weaponAmmo.ContainsKey(key))
            _weaponAmmo[key] = 0;

        // Don't add to infinite ammo weapons (-1)
        if (_weaponAmmo[key] == -1)
            return;

        _weaponAmmo[key] += amount;
    }

    public bool ConsumeAmmo(WeaponType weaponType, int amount = 1)
    {
        if (!IsWeaponUnlocked(weaponType))
            return false;

        int key = (int)weaponType;
        if (!_weaponAmmo.ContainsKey(key))
            return false;

        // Infinite ammo
        if (_weaponAmmo[key] == -1)
            return true;

        if (_weaponAmmo[key] < amount)
            return false;

        _weaponAmmo[key] -= amount;
        return true;
    }

    public int GetAmmo(WeaponType weaponType)
    {
        int key = (int)weaponType;
        if (_weaponAmmo.ContainsKey(key))
            return _weaponAmmo[key];
        return 0;
    }

    public bool CanUseWeapon(WeaponType weaponType)
    {
        if (!IsWeaponUnlocked(weaponType))
            return false;

        int key = (int)weaponType;
        if (!_weaponAmmo.ContainsKey(key))
            return false;

        return _weaponAmmo[key] == -1 || _weaponAmmo[key] > 0;
    }

    public WeaponType[] GetUnlockedWeapons()
    {
        return _unlockedWeapons
            .Where(kvp => kvp.Value)
            .Select(kvp => (WeaponType)kvp.Key)
            .ToArray();
    }

    // ── Ability Management ─────────────────────────────────────

    public void UnlockAbility(AbilityType abilityType)
    {
        int key = (int)abilityType;
        if (!_unlockedAbilities.ContainsKey(key))
        {
            _unlockedAbilities[key] = false;
        }

        if (!_unlockedAbilities[key])
        {
            _unlockedAbilities[key] = true;
            GD.Print($"Ability unlocked: {abilityType}");
        }
    }

    public bool HasAbility(AbilityType abilityType)
    {
        int key = (int)abilityType;
        return _unlockedAbilities.ContainsKey(key) && _unlockedAbilities[key];
    }

    // ── Inventory Management ───────────────────────────────────

    public void AddItem(ItemType itemType, int quantity = 1)
    {
        int key = (int)itemType;
        if (!_inventory.ContainsKey(key))
            _inventory[key] = 0;

        _inventory[key] += quantity;
    }

    public bool RemoveItem(ItemType itemType, int quantity = 1)
    {
        int key = (int)itemType;
        if (!_inventory.ContainsKey(key) || _inventory[key] < quantity)
            return false;

        _inventory[key] -= quantity;

        if (_inventory[key] <= 0)
            _inventory.Remove(key);

        return true;
    }

    public int GetItemCount(ItemType itemType)
    {
        int key = (int)itemType;
        if (_inventory.ContainsKey(key))
            return _inventory[key];
        return 0;
    }

    public bool HasItem(ItemType itemType)
    {
        int key = (int)itemType;
        return _inventory.ContainsKey(key) && _inventory[key] > 0;
    }

    // ── ISaveableState Implementation ──────────────────────────

    Resource ISaveableState.ToSaveData() => ToSaveData();
    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData
        {
            LastCheckpointId = LastCheckpointId,
            LastCheckpointLevelId = LastCheckpointLevelId,
            CurrentHealth = CurrentHealth,
            MaxHealth = MaxHealth,
            Armor = Armor,
            Coins = Coins,
            UnlockedWeapons = new(_unlockedWeapons),
            WeaponAmmo = new(_weaponAmmo),
            UnlockedAbilities = new(_unlockedAbilities),
            Inventory = new(_inventory)
        };
    }

    void ISaveableState.LoadFromSaveData(Resource data) => LoadFromSaveData((PlayerSaveData)data);
    public void LoadFromSaveData(PlayerSaveData data)
    {
        if (data == null) return;

        LastCheckpointId = data.LastCheckpointId;
        LastCheckpointLevelId = data.LastCheckpointLevelId;
        CurrentHealth = data.CurrentHealth;
        MaxHealth = data.MaxHealth;
        Armor = data.Armor;
        Coins = data.Coins;
        _unlockedWeapons = new(data.UnlockedWeapons);
        _weaponAmmo = new(data.WeaponAmmo);
        _unlockedAbilities = new(data.UnlockedAbilities);
        _inventory = new(data.Inventory);
    }

    public void ResetToDefaults()
    {
        CurrentHealth = 100f;
        MaxHealth = 100f;
        Armor = 0;
        Coins = 0;
        LastCheckpointId = "";
        LastCheckpointLevelId = "";

        _unlockedWeapons.Clear();
        _unlockedWeapons[(int)WeaponType.Pistol] = true;
        _weaponAmmo.Clear();
        _weaponAmmo[(int)WeaponType.Pistol] = -1; // Infinite

        _unlockedAbilities.Clear();
        _inventory.Clear();
    }

}
