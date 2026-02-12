using Godot;

[GlobalClass]
public partial class PlayerSaveData : Resource
{
    [Export] public string LastCheckpointId = "";
    [Export] public string LastCheckpointLevelId = "";
    [Export] public float CurrentHealth = 100f;
    [Export] public float MaxHealth = 100f;
    [Export] public int Coins;
    // Future: equipped weapons, unlocked abilities, etc.
}
