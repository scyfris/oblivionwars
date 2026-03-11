using Godot;

[GlobalClass]
public partial class LevelSaveData : Resource
{
    [Export] public string LevelId = "";
    [Export] public Godot.Collections.Array<string> ActivatedCheckpointIds = new();
    [Export] public Godot.Collections.Array<string> UpgradedCheckpointIds = new();
    [Export] public Godot.Collections.Array<string> DestroyedObjectIds = new();
    [Export] public Godot.Collections.Array<string> UnlockedDoorIds = new();

    /// <summary>
    /// Generic object state storage. Key = UniqueId, Value = LevelObjectSaveDataEntry subclass.
    /// Objects define their own subclasses (DoorSaveData, CheckpointSaveData, etc.) that inherit from LevelObjectSaveDataEntry.
    /// </summary>
    [Export] public Godot.Collections.Dictionary<string, LevelObjectSaveDataEntry> ObjectStates = new();
}
