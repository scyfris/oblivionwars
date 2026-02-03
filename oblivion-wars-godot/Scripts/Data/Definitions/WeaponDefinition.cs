using Godot;

[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public string WeaponId = "";
    [Export] public float UseCooldown = 0.2f;
    [Export] public bool IsAutomatic = true;
    [Export] public float DamageScale = 1.0f;
    [Export] public float Knockback = 100.0f;
    [Export] public float ScreenShake = 1.5f;
    [Export] public Vector2 ProjectileSpawnOffset = new Vector2(20, 0);

    [ExportGroup("Projectile")]
    [Export] public ProjectileDefinition Projectile;

    [ExportGroup("Scene")]
    [Export] public PackedScene WeaponScene;

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;
    [Export] public float SpreadAngle = 15.0f;
}
