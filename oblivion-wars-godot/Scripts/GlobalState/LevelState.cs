using Godot;

public partial class LevelState : Node
{
    public static LevelState Instance { get; private set; }

    public LevelDefinition CurrentLevel;
    public LevelSaveData SaveData = new();

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("LevelState: Duplicate instance detected, removing this one.");
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

    public void LoadForLevel(string levelId, LevelSaveData data)
    {
        SaveData = data ?? new LevelSaveData();
        SaveData.LevelId = levelId;
    }

    public void Reset()
    {
        CurrentLevel = null;
        SaveData = new LevelSaveData();
    }

    // ── Query Methods ──────────────────────────────────────

    public bool IsCheckpointActivated(string checkpointId)
    {
        return SaveData.ActivatedCheckpointIds.Contains(checkpointId);
    }

    public bool IsCheckpointUpgraded(string checkpointId)
    {
        return SaveData.UpgradedCheckpointIds.Contains(checkpointId);
    }

    public bool IsObjectDestroyed(string objectId)
    {
        return SaveData.DestroyedObjectIds.Contains(objectId);
    }

    public bool IsDoorUnlocked(string doorId)
    {
        return SaveData.UnlockedDoorIds.Contains(doorId);
    }

    // ── Mutation Methods ───────────────────────────────────

    public void ActivateCheckpoint(string checkpointId)
    {
        if (!SaveData.ActivatedCheckpointIds.Contains(checkpointId))
            SaveData.ActivatedCheckpointIds.Add(checkpointId);
    }

    public void UpgradeCheckpoint(string checkpointId)
    {
        if (!SaveData.UpgradedCheckpointIds.Contains(checkpointId))
            SaveData.UpgradedCheckpointIds.Add(checkpointId);
    }

    public void MarkObjectDestroyed(string objectId)
    {
        if (!SaveData.DestroyedObjectIds.Contains(objectId))
            SaveData.DestroyedObjectIds.Add(objectId);
    }

    public void UnlockDoor(string doorId)
    {
        if (!SaveData.UnlockedDoorIds.Contains(doorId))
            SaveData.UnlockedDoorIds.Add(doorId);
    }

    // ── Generic Object State Methods ──────────────────────────

    /// <summary>
    /// Save custom state for any saveable object.
    /// </summary>
    public void SaveObjectState(string uniqueId, LevelObjectSaveDataEntry saveData)
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            GD.PrintErr("LevelState: Cannot save object state with empty UniqueId");
            return;
        }

        if (saveData == null)
        {
            GD.PrintErr("LevelState: Cannot save null saveData");
            return;
        }

        SaveData.ObjectStates[uniqueId] = saveData;
    }

    /// <summary>
    /// Load custom state for a saveable object by its UniqueId.
    /// Returns null if no saved state exists.
    /// </summary>
    public LevelObjectSaveDataEntry LoadObjectState(string uniqueId)
    {
        if (SaveData.ObjectStates.TryGetValue(uniqueId, out var entry))
        {
            return entry;
        }
        return null;
    }

    /// <summary>
    /// Query all saved objects of a specific type.
    /// Returns list of UniqueIds for objects of that type.
    /// </summary>
    public Godot.Collections.Array<string> QueryObjectsByType(SaveableLevelObjectType objectType)
    {
        var results = new Godot.Collections.Array<string>();
        foreach (var kvp in SaveData.ObjectStates)
        {
            if (kvp.Value.ObjectType == objectType)
            {
                results.Add(kvp.Key);
            }
        }
        return results;
    }

    /// <summary>
    /// Check if an object has saved state.
    /// </summary>
    public bool HasObjectState(string uniqueId)
    {
        return SaveData.ObjectStates.ContainsKey(uniqueId);
    }

    /// <summary>
    /// Remove saved state for an object (e.g., when object is destroyed permanently).
    /// </summary>
    public void RemoveObjectState(string uniqueId)
    {
        SaveData.ObjectStates.Remove(uniqueId);
    }
}
