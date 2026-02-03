using Godot;

[GlobalClass]
public partial class ProjectileDefinition : Resource
{
    [Export] public string ProjectileId = "";

    [ExportGroup("Behavior")]
    [Export] public float Speed = 800.0f;
    [Export] public float Lifetime = 3.0f;
    [Export] public float Damage = 10.0f;
    [Export] public bool BounceOffWalls = false;
    [Export] public int MaxBounces = 0;
    [Export] public bool AffectedByGravity = false;
    [Export] public float GravityScale = 1.0f;

    [ExportGroup("Raycast")]
    [Export] public float HitscanRange = 1000.0f;
    [Export] public float TrailDuration = 0.1f;

    [ExportGroup("Explosion")]
    [Export] public float ExplosionRadius = 0.0f;
    [Export] public float FuseTime = 0.0f;

    [ExportGroup("Visuals")]
    [Export] public PackedScene ProjectileScene;
}
