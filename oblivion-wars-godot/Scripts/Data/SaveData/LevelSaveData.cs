using Godot;

[GlobalClass]
public partial class LevelSaveData : Resource
{
    [Export] public string LevelId = "";
    [Export] public Godot.Collections.Array<int> ActivatedCheckpointIds = new();
    [Export] public Godot.Collections.Array<int> UpgradedCheckpointIds = new();
    [Export] public Godot.Collections.Array<string> DestroyedObjectIds = new();
    [Export] public Godot.Collections.Array<string> UnlockedDoorIds = new();
    // Future: any level-specific persistent state
}
