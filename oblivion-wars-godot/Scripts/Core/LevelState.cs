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

    public bool IsCheckpointActivated(uint checkpointId)
    {
        return SaveData.ActivatedCheckpointIds.Contains((int)checkpointId);
    }

    public bool IsCheckpointUpgraded(uint checkpointId)
    {
        return SaveData.UpgradedCheckpointIds.Contains((int)checkpointId);
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

    public void ActivateCheckpoint(uint checkpointId)
    {
        int id = (int)checkpointId;
        if (!SaveData.ActivatedCheckpointIds.Contains(id))
            SaveData.ActivatedCheckpointIds.Add(id);
    }

    public void UpgradeCheckpoint(uint checkpointId)
    {
        int id = (int)checkpointId;
        if (!SaveData.UpgradedCheckpointIds.Contains(id))
            SaveData.UpgradedCheckpointIds.Add(id);
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
}
