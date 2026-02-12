using Godot;

[GlobalClass]
public partial class GlobalSaveData : Resource
{
    [Export] public Godot.Collections.Array<string> DefeatedBossIds = new();
    // Future: global world flags, story progress, etc.
}
