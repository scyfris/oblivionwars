using Godot;

/// <summary>
/// Custom save data for Door objects.
/// Stores whether the door is unlocked.
/// </summary>
[GlobalClass]
public partial class DoorSaveData : LevelObjectSaveDataEntry
{
    [Export] public bool IsUnlocked = false;

    public DoorSaveData()
    {
        ObjectType = SaveableLevelObjectType.Door;
    }
}
