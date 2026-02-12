using Godot;

/// <summary>
/// Base class for all level object save data.
/// Subclass this for specific object types (DoorSaveData, CheckpointSaveData, etc.)
/// Stored in LevelSaveData with the object's unique ID as the key.
/// </summary>
[GlobalClass]
public partial class LevelObjectSaveDataEntry : Resource
{
    /// <summary>
    /// Type of the saveable object (Door, Lever, Chest, etc.)
    /// Set by subclass constructors.
    /// </summary>
    public SaveableLevelObjectType ObjectType = SaveableLevelObjectType.Unknown;
}
