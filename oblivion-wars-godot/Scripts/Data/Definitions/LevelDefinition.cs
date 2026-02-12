using Godot;

[GlobalClass]
public partial class LevelDefinition : Resource
{
    [Export] public string LevelId = "";
    [Export] public string LevelDisplayName = "";
    [Export] public uint DefaultCheckpointId = 0;
    [Export] public string ScenePath = "";  // res:// path to the level scene
    // Future: music, environment settings, loading screen, etc.
}
