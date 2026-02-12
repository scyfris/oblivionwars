using Godot;

[GlobalClass]
public partial class DropTableEntry : Resource
{
    [Export] public PackedScene DropScene;
    [Export] public float DropChance = 1.0f;
    [Export] public int MinCount = 1;
    [Export] public int MaxCount = 1;
    [Export] public string RequiredUnlockId = "";
}
