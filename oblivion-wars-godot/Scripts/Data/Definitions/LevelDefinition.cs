using Godot;

[GlobalClass]
public partial class LevelDefinition : Resource
{
    [Export] public string LevelId = "";
    [Export] public string LevelDisplayName = "";
    [Export] public uint DefaultCheckpointId = 0;
    [Export(PropertyHint.File, "*.tscn")] public string ScenePath = "";
    // Future: music, environment settings, loading screen, etc.
}
