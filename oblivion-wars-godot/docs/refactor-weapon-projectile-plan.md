# Refactor: Move Combat Params to Weapon, Visuals to Projectile Scene

## Context
Combat parameters (damage, speed, lifetime) currently live on `ProjectileDefinition` resource but belong to the weapon. Visual/trail config also lives there but belongs on the projectile scene itself. This refactor:
- Moves combat params → `WeaponDefinition`
- Moves visual/trail params → `[Export]` fields on `Projectile.cs` (configured per-scene in inspector)
- **Deletes `ProjectileDefinition.cs` entirely** — no more separate resource

## New: `ProjectileParams` struct
**New file:** `Scripts/Combat/Projectiles/ProjectileParams.cs`

```csharp
public struct ProjectileParams
{
    public float Speed;
    public float Damage;
    public float Lifetime;
    public float HitscanRange;
    public bool AffectedByGravity;
    public float GravityScale;
}
```

Weapon builds this struct and passes it to `Projectile.Initialize()`. Future weapon mods modify the struct values before passing them in.

## WeaponDefinition.cs — gains combat params + projectile scene

Add new fields, remove old ones:

```csharp
[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public float UseCooldown = 0.2f;
    [Export] public bool IsAutomatic = true;
    [Export] public float ScreenShakeScale = 1.0f;
    [Export] public float ScreenShakeDurationScale = 1.0f;

    [ExportGroup("Combat")]
    [Export] public float Damage = 10.0f;
    [Export] public float Speed = 800.0f;
    [Export] public float Lifetime = 3.0f;
    [Export] public float HitscanRange = 1000.0f;
    [Export] public bool AffectedByGravity = false;
    [Export] public float GravityScale = 1.0f;

    [ExportGroup("Projectile")]
    [Export] public PackedScene ProjectileScene;

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;
    [Export] public float SpreadAngle = 15.0f;
}
```

**Removed:** `DamageScale` (replaced by flat `Damage`), `Knockback` (unused), `Projectile` (ProjectileDefinition reference — deleted entirely)

## Delete ProjectileDefinition entirely

- Delete `Scripts/Data/Definitions/ProjectileDefinition.cs`
- Delete `Resources/Data/Projectiles/bullet_projectile.tres`
- Delete `Resources/Data/Projectiles/raycast_bullet_projectile.tres`

## Projectile.cs — visual exports + new Initialize

Trail/visual config becomes `[Export]` fields on the base class, configured per .tscn scene in inspector:

```csharp
[Export] protected Line2D _trail;  // already exists

[ExportGroup("Hitscan Trail")]
[Export] protected float _trailDuration = 0.1f;
[Export] protected bool _trailUseSweep = true;
[Export(PropertyHint.Range, "0,10,0.1")] protected float _trailWidenScale = 3f;
[Export] protected Gradient _trailGradient;

[ExportGroup("Trail")]
[Export] protected int _trailLength = 10;
```

New Initialize signature:
```csharp
public virtual void Initialize(Vector2 direction, ProjectileParams projectileParams, Node2D shooter = null)
{
    _direction = direction.Normalized();
    _params = projectileParams;
    _shooter = shooter;
    Rotation = direction.Angle();
}
```

Key field changes:
- Remove `_projectileDefinition` field entirely
- Remove `_damage` field
- Add `protected ProjectileParams _params`
- Lifetime reads from `_params.Lifetime` (not null-checked, it's a struct)
- Trail reads from direct fields: `_trailLength`, `_trailDuration`, `_trailWidenScale`, etc.

All `_projectileDefinition?.X ?? default` patterns become direct field reads:
| Old | New |
|-----|-----|
| `_projectileDefinition?.TrailDuration ?? 0.1f` | `_trailDuration` |
| `_projectileDefinition?.TrailGradient` | `_trailGradient` |
| `_projectileDefinition?.TrailLength ?? 10` | `_trailLength` |
| `_projectileDefinition?.TrailWidenScale ?? 3f` | `_trailWidenScale` |
| `_projectileDefinition?.TrailUseSweep ?? true` | `_trailUseSweep` |
| `_projectileDefinition?.Lifetime ?? 3.0f` | `_params.Lifetime` |

## StandardBullet.cs — use _params for combat values

```csharp
public partial class StandardBullet : Projectile
{
    public override void Initialize(Vector2 direction, ProjectileParams projectileParams, Node2D shooter = null)
    {
        base.Initialize(direction, projectileParams, shooter);

        if (_params.Speed == 0 && shooter != null)
            PerformHitscan();
    }

    protected override void UpdateMovement(double delta)
    {
        GlobalPosition += _direction * _params.Speed * (float)delta;
    }

    protected override void OnHit(Node2D body)
    {
        if (body == null || !GodotObject.IsInstanceValid(body))
            return;
        RaiseHitEvent(body, GlobalPosition);
    }

    private void PerformHitscan()
    {
        Vector2 from = GlobalPosition;
        Vector2 to = from + _direction * _params.HitscanRange;
        // ... raycast logic unchanged, just uses _params.Damage in RaiseHitEvent
    }

    private void RaiseHitEvent(Node2D target, Vector2 hitPosition)
    {
        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = target.GetInstanceId(),
            SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
            BaseDamage = _params.Damage,
            HitDirection = _direction,
            HitPosition = hitPosition,
        });
    }
}
```

Subclasses can add their own `[Export]` fields for special visual behavior.

## HitEvent (CombatEvents.cs) — remove Projectile field

The `Projectile` field (type `ProjectileDefinition`) is set but **never read** by any consumer. Remove it:

```csharp
public struct HitEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public ulong SourceInstanceId;
    public float BaseDamage;
    public Vector2 HitDirection;
    public Vector2 HitPosition;
}
```

## Weapon.cs — builds ProjectileParams, simplified

```csharp
private void TryFire()
{
    if (!CanUse() || _weaponDefinition?.ProjectileScene == null) return;
    _hasFiredThisPress = true;

    FireProjectile();
    ResetCooldown();
    _animationPlayer?.Play("shoot");

    if (_owner is PlayerCharacterBody2D && CameraController.Instance != null && _weaponDefinition.ScreenShakeScale > 0)
        CameraController.Instance.Shake(_weaponDefinition.ScreenShakeScale, _weaponDefinition.ScreenShakeDurationScale);
}

private void FireProjectile()
{
    Vector2 baseDirection = AimDirection;
    if (_weaponDefinition.SpreadCount <= 1)
    {
        SpawnProjectile(baseDirection);
    }
    else
    {
        // same spread angle logic, just calls SpawnProjectile(spreadDir)
    }
}

private void SpawnProjectile(Vector2 direction)
{
    var projectile = _weaponDefinition.ProjectileScene.Instantiate<Projectile>();
    projectile.GlobalPosition = GetSpawnPosition();

    var projectileParams = new ProjectileParams
    {
        Speed = _weaponDefinition.Speed,
        Damage = _weaponDefinition.Damage,
        Lifetime = _weaponDefinition.Lifetime,
        HitscanRange = _weaponDefinition.HitscanRange,
        AffectedByGravity = _weaponDefinition.AffectedByGravity,
        GravityScale = _weaponDefinition.GravityScale,
    };

    projectile.Initialize(direction, projectileParams, _owner);
    _owner.GetParent().AddChild(projectile);
}
```

## .tres file updates

### pistol_settings.tres
- Add: `Damage=10.0`, `Speed=2000.0`, `Lifetime=3.0`, `HitscanRange=1000.0`
- Add: `ProjectileScene` → `res://Scenes/Projectiles/StandardBullet.tscn`
- Remove: `DamageScale`, `Projectile` (reference to deleted bullet_projectile.tres)

### shotgun_settings.tres
- Add: `Damage=5.0`, `Speed=2000.0`, `Lifetime=3.0`, `HitscanRange=1000.0`
- Add: `ProjectileScene` → `res://Scenes/Projectiles/StandardBullet.tscn`
- Remove: `DamageScale`, `Projectile` (reference to deleted bullet_projectile.tres)

### StandardBullet.tscn
Set the new export values on the root node:
- `_trailDuration = 0.5`
- `_trailWidenScale = 2.7`
- `_trailLength = 10`

Note: Both pistol and shotgun share the same StandardBullet scene, so they share trail config. If different weapons need different trail looks, create separate projectile scenes.

## Implementation order

1. **Create** `Scripts/Combat/Projectiles/ProjectileParams.cs` — new struct, nothing depends on it yet
2. **Update** `Projectile.cs` — add visual [Export] fields, new Initialize signature, replace `_projectileDefinition` with direct fields and `_params`
3. **Update** `StandardBullet.cs` — update Initialize override, use `_params` for combat values
4. **Update** `CombatEvents.cs` — remove `Projectile` field from HitEvent
5. **Update** `WeaponDefinition.cs` — add Combat fields, `ProjectileScene`, remove `DamageScale`/`Knockback`/`Projectile`
6. **Update** `Weapon.cs` — build `ProjectileParams`, simplified flow
7. **Delete** `ProjectileDefinition.cs` and both `.tres` projectile files
8. **Update** weapon `.tres` files (pistol_settings.tres, shotgun_settings.tres)
9. **Update** `StandardBullet.tscn` — set trail export values in scene

## Verification
- Build succeeds with no errors
- Pistol: physical bullets at speed 2000, damage 10, trail renders
- Shotgun: 5 spread pellets at speed 2000, damage 5 each
- Hitscan: works with Speed=0 on a weapon (change pistol Speed to 0 to test)
- Trail effects (sweep, fade, widen) still render correctly
- EffectsSystem hit effects still trigger on enemy/wall hits
