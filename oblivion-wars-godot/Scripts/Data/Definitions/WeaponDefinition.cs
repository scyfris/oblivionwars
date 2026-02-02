using Godot;

[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public string WeaponId = "";
    [Export] public float Damage = 10.0f;
    [Export] public float FireRate = 0.2f;
    [Export] public float Knockback = 100.0f;
    [Export] public float ScreenShake = 1.5f;
    [Export] public PackedScene ProjectileScene;
    [Export] public float HitscanRange = 1000.0f;
    [Export] public float UseCooldown = 0.2f;
    [Export] public Vector2 ProjectileSpawnOffset = new Vector2(20, 0);
    [Export] public float TrailDuration = 0.1f;

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;
    [Export] public float SpreadAngle = 15.0f;
}
