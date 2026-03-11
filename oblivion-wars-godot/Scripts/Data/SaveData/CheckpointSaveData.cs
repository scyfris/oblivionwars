using Godot;

/// <summary>
/// Custom save data for Checkpoint objects.
/// Stores activation and upgrade status.
/// </summary>
[GlobalClass]
public partial class CheckpointSaveData : LevelObjectSaveDataEntry
{
    [Export] public bool IsActivated = false;
    [Export] public bool IsUpgraded = false;

    public CheckpointSaveData()
    {
        ObjectType = SaveableLevelObjectType.Checkpoint;
    }
}
