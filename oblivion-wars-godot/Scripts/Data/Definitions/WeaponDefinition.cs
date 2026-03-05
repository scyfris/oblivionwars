using Godot;

[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public float FireRate = 0.2f;
    [Export] public bool IsAutomatic = true;
    [Export] public float ScreenShakeScale = 1.0f;
    [Export] public float ScreenShakeDurationScale = 1.0f;

    [ExportGroup("Combat")]
    [Export] public float Damage = 10.0f;
    [Export] public float Speed = 800.0f;
    [Export] public float Lifetime = 3.0f;
    /// <summary>Knockback impulse applied to hit targets. 0 = no knockback.</summary>
    [Export] public float ImpactForce = 0.0f;
    [Export] public bool ProjectileAffectedByGravity = false;
    [Export] public float ProjectileGravityScale = 1.0f;

    [ExportGroup("Projectile")]
    [Export] public PackedScene ProjectileScene;

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;
    [Export] public float SpreadAngle = 15.0f;

    [ExportGroup("Explosive")]
    [Export] public bool Explosive = false;
    [Export] public float ExplosionRadius = 100.0f;
    /// <summary>Minimum damage fraction at the edge of the explosion radius (1.0 = no falloff).</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float ExplosionDamageFalloff = 0.25f;
}
