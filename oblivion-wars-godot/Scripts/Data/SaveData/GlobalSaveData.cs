using Godot;

[GlobalClass]
public partial class GlobalSaveData : Resource
{
    [Export] public Godot.Collections.Array<string> DefeatedBossIds = new();
    [Export] public Godot.Collections.Dictionary<string, bool> GlobalFlags = new();
    // Future: global world flags, story progress, etc.
}
