using Godot;

public partial class PlayerState : Node
{
    public static PlayerState Instance { get; private set; }

    // Persistent data (saved to disk)
    public float CurrentHealth = 100f;
    public float MaxHealth = 100f;
    public int Coins;
    public string LastCheckpointId = "";
    public string LastCheckpointLevelId = "";
    // Future: equipped weapons, unlocked abilities, inventory, etc.

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("PlayerState: Duplicate instance detected, removing this one.");
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

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData
        {
            LastCheckpointId = LastCheckpointId,
            LastCheckpointLevelId = LastCheckpointLevelId,
            CurrentHealth = CurrentHealth,
            MaxHealth = MaxHealth,
            Coins = Coins
        };
    }

    public void LoadFromSaveData(PlayerSaveData data)
    {
        if (data == null) return;

        LastCheckpointId = data.LastCheckpointId;
        LastCheckpointLevelId = data.LastCheckpointLevelId;
        CurrentHealth = data.CurrentHealth;
        MaxHealth = data.MaxHealth;
        Coins = data.Coins;
    }

    public void ResetToDefaults(PlayerDefinition definition, string startingLevelId, string startingCheckpointId)
    {
        CurrentHealth = definition?.MaxHealth ?? 100f;
        MaxHealth = definition?.MaxHealth ?? 100f;
        Coins = 0;
        LastCheckpointId = startingCheckpointId;
        LastCheckpointLevelId = startingLevelId;
    }

    /// <summary>
    /// Check if the player has a specific unlock. Placeholder for future unlock/ability system.
    /// </summary>
    public bool HasUnlock(string unlockId)
    {
        // TODO: Implement when ability/unlock system is built
        return false;
    }
}
