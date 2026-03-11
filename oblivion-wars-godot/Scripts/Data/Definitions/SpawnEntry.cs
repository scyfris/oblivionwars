using Godot;

[GlobalClass]
public partial class SpawnEntry : Resource
{
    [Export] public PackedScene EnemyScene;
    [Export(PropertyHint.Range, "0.1,100,0.1")] public float Weight = 1.0f;
}
