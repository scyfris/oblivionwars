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

    // Current equipped weapon (by registry name)
    public string CurrentWeaponId = "";

    // Weapons (weaponName -> unlocked)
    private Godot.Collections.Dictionary<string, bool> _unlockedWeapons = new();

    // Ammo (weaponName -> ammo count, -1 = infinite)
    private Godot.Collections.Dictionary<string, int> _weaponAmmo = new();

    // Abilities (AbilityType -> unlocked)
    private Godot.Collections.Dictionary<int, bool> _unlockedAbilities = new();

    // Inventory items (ItemType -> quantity)
    private Godot.Collections.Dictionary<int, int> _inventory = new();

    // ── Weapon Management ──────────────────────────────────────

    public void UnlockWeapon(string weaponId, int startingAmmo = 0)
    {
        if (string.IsNullOrEmpty(weaponId)) return;

        if (!_unlockedWeapons.ContainsKey(weaponId))
            _unlockedWeapons[weaponId] = false;

        if (!_unlockedWeapons[weaponId])
        {
            _unlockedWeapons[weaponId] = true;
            _weaponAmmo[weaponId] = startingAmmo;
            GD.Print($"Weapon unlocked: {weaponId}");
        }
    }

    public bool IsWeaponUnlocked(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return false;
        return _unlockedWeapons.ContainsKey(weaponId) && _unlockedWeapons[weaponId];
    }

    public void AddAmmo(string weaponId, int amount)
    {
        if (!IsWeaponUnlocked(weaponId)) return;

        if (!_weaponAmmo.ContainsKey(weaponId))
            _weaponAmmo[weaponId] = 0;

        // Don't add to infinite ammo weapons (-1)
        if (_weaponAmmo[weaponId] == -1) return;

        _weaponAmmo[weaponId] += amount;
    }

    public bool ConsumeAmmo(string weaponId, int amount = 1)
    {
        if (!IsWeaponUnlocked(weaponId)) return false;

        if (!_weaponAmmo.ContainsKey(weaponId)) return false;

        // Infinite ammo
        if (_weaponAmmo[weaponId] == -1) return true;

        if (_weaponAmmo[weaponId] < amount) return false;

        _weaponAmmo[weaponId] -= amount;
        return true;
    }

    public int GetAmmo(string weaponId)
    {
        if (!string.IsNullOrEmpty(weaponId) && _weaponAmmo.ContainsKey(weaponId))
            return _weaponAmmo[weaponId];
        return 0;
    }

    public bool CanUseWeapon(string weaponId)
    {
        if (!IsWeaponUnlocked(weaponId)) return false;

        if (!_weaponAmmo.ContainsKey(weaponId)) return false;

        return _weaponAmmo[weaponId] == -1 || _weaponAmmo[weaponId] > 0;
    }

    public string[] GetUnlockedWeapons()
    {
        return _unlockedWeapons
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
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
            CurrentWeaponId = CurrentWeaponId,
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
        CurrentWeaponId = data.CurrentWeaponId;
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

        // Default weapon comes from registry — unlock it with infinite ammo
        var defaultWeapon = GlobalDefinitions.Instance?.GetDefaultWeaponName() ?? "";
        CurrentWeaponId = defaultWeapon;

        _unlockedWeapons.Clear();
        _weaponAmmo.Clear();
        if (!string.IsNullOrEmpty(defaultWeapon))
        {
            _unlockedWeapons[defaultWeapon] = true;
            _weaponAmmo[defaultWeapon] = -1; // Infinite
        }

        _unlockedAbilities.Clear();
        _inventory.Clear();
    }

}
