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
    [Export] public bool EnableWallBounce = false;
    [Export] public bool EnableEnemyBounce = false;
    [Export] public int MaxBounces = 0;
    /// <summary>Speed retained after each bounce. 1.0 = fully elastic, 0.5 = loses half speed per bounce.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float CoefficientOfRestitution = 0.8f;

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;
    [Export] public float SpreadAngle = 15.0f;

    [ExportGroup("Explosive")]
    [Export] public bool Explosive = false;
    [Export] public float ExplosionRadius = 100.0f;
    /// <summary>Minimum damage fraction at the edge of the explosion radius (1.0 = no falloff).</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float ExplosionDamageFalloff = 0.25f;
    /// <summary>If > 0, projectile explodes after this many seconds instead of on impact.</summary>
    [Export] public float TimedExplosion = 0f;
    /// <summary>When TimedExplosion > 0, immediately explode on enemy contact instead of waiting for timer.</summary>
    [Export] public bool CancelTimedOnEnemyContact = false;
}
