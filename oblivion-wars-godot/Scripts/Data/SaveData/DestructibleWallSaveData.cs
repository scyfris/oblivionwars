using Godot;

/// <summary>
/// Custom save data for DestructibleWall objects.
/// Tracks destruction state and can store additional info like damage taken.
/// </summary>
[GlobalClass]
public partial class DestructibleWallSaveData : LevelObjectSaveDataEntry
{
    [Export] public bool IsDestroyed = false;
    [Export] public float CurrentHealth = 100f;
    [Export] public int TimesHit = 0;  // Example of custom property specific to this object type

    public DestructibleWallSaveData()
    {
        ObjectType = SaveableLevelObjectType.DestructibleWall;
    }
}
