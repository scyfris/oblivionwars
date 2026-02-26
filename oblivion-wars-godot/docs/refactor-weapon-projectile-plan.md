# Refactor: Move Combat Params to Weapon, CCD Projectile Movement

## Context
Combat parameters (damage, speed, lifetime) currently live on `ProjectileDefinition` resource but belong to the weapon. This refactor:
- Moves combat params → `WeaponDefinition`
- Moves visual/trail params → `[Export]` fields on `Projectile.cs`
- **Deletes `ProjectileDefinition.cs` entirely**
- **Eliminates hitscan as a concept** — replaces with continuous collision detection (CCD) via per-frame raycast. "Hitscan" is just a very fast bullet (e.g. speed=100000).
- Trail rework (particles) deferred to a separate step.

## New: `ProjectileParams` struct
**New file:** `Scripts/Combat/Projectiles/ProjectileParams.cs`

```csharp
using Godot;

public struct ProjectileParams
{
    public float Speed;
    public float Damage;
    public float Lifetime;
    public bool AffectedByGravity;
    public float GravityScale;
}
```

## WeaponDefinition.cs — gains combat params + projectile scene

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
    [Export] public bool AffectedByGravity = false;
    [Export] public float GravityScale = 1.0f;

    [ExportGroup("Projectile")]
    [Export] public PackedScene ProjectileScene;

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;
    [Export] public float SpreadAngle = 15.0f;
}
```

**Removed:** `DamageScale`, `Knockback` (unused), `Projectile` (ProjectileDefinition ref), `HitscanRange`

## Delete ProjectileDefinition entirely

- Delete `Scripts/Data/Definitions/ProjectileDefinition.cs`
- Delete `Resources/Data/Projectiles/bullet_projectile.tres`
- Delete `Resources/Data/Projectiles/raycast_bullet_projectile.tres`

## Projectile.cs — CCD movement + visual exports

Remove ALL hitscan trail logic: `_isHitscanTrail`, `_hitscanTrailTimer`, `_hitscanTrailDuration`, `_hitscanTrailInitialWidth`, `_hitscanFrom`, `_hitscanTo`, `InitializeAsHitscanTrail()`, `UpdateHitscanTrail()`.

Remove `_OnBodyEntered` / `BodyEntered` signal — no longer using Area2D overlap for hit detection.

Trail visual config as `[Export]` fields (kept minimal for now, particle rework later):

```csharp
[Export] protected Line2D _trail;  // already exists

[ExportGroup("Trail")]
[Export] protected int _trailLength = 10;
```

New Initialize:
```csharp
public virtual void Initialize(Vector2 direction, ProjectileParams projectileParams, Node2D shooter = null)
{
    _direction = direction.Normalized();
    _params = projectileParams;
    _shooter = shooter;
    Rotation = direction.Angle();
}
```

New `_PhysicsProcess` with CCD:
```csharp
public override void _PhysicsProcess(double delta)
{
    UpdateMovement(delta);
    UpdateTrail();
    UpdateLifetime(delta);
}
```

The base class `UpdateMovement` is no longer abstract — it becomes a virtual method with CCD built in:
```csharp
protected virtual void UpdateMovement(double delta)
{
    Vector2 velocity = _direction * _params.Speed * (float)delta;
    Vector2 nextPosition = GlobalPosition + velocity;

    // CCD: raycast from current to next position to catch collisions
    var spaceState = GetWorld2D().DirectSpaceState;
    var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, nextPosition);
    if (_shooter is CollisionObject2D col)
        query.Exclude = new Godot.Collections.Array<Rid> { col.GetRid() };

    var result = spaceState.IntersectRay(query);
    if (result.Count > 0)
    {
        var hitBody = (Node2D)result["collider"];
        var hitPosition = (Vector2)result["position"];
        GlobalPosition = hitPosition;

        if (hitBody != _shooter && GodotObject.IsInstanceValid(hitBody) && !hitBody.IsQueuedForDeletion())
        {
            OnHit(hitBody);
            QueueFree();
            return;
        }
    }
    else
    {
        GlobalPosition = nextPosition;
    }
}
```

Field changes:
- Remove `_projectileDefinition` field
- Remove `_damage` field
- Add `protected ProjectileParams _params`
- Lifetime: `_params.Lifetime`

`UpdateTrail()` stays the same (rolling point buffer with `_trailLength`).

`UpdateLifetime()` uses `_params.Lifetime`.

`OnHit(Node2D body)` stays as a virtual method for subclasses.

## StandardBullet.cs — drastically simplified

```csharp
using Godot;

public partial class StandardBullet : Projectile
{
    protected override void OnHit(Node2D body)
    {
        if (body == null || !GodotObject.IsInstanceValid(body))
            return;

        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = body.GetInstanceId(),
            SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
            BaseDamage = _params.Damage,
            HitDirection = _direction,
            HitPosition = GlobalPosition,
        });
    }
}
```

No more `UpdateMovement` override, `PerformHitscan`, `RaiseHitEvent`, or Initialize override.

## HitEvent (CombatEvents.cs) — remove Projectile field

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

Also update `HazardSystem.cs` — remove `Projectile = null` line from HitEvent creation.

## Weapon.cs — simple, no hitscan

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
        float totalAngle = Mathf.DegToRad(_weaponDefinition.SpreadAngle);
        float startAngle = -totalAngle / 2f;
        float step = _weaponDefinition.SpreadCount > 1
            ? totalAngle / (_weaponDefinition.SpreadCount - 1)
            : 0f;

        for (int i = 0; i < _weaponDefinition.SpreadCount; i++)
        {
            float angle = startAngle + step * i;
            SpawnProjectile(baseDirection.Rotated(angle));
        }
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
        AffectedByGravity = _weaponDefinition.AffectedByGravity,
        GravityScale = _weaponDefinition.GravityScale,
    };

    projectile.Initialize(direction, projectileParams, _owner);
    _owner.GetParent().AddChild(projectile);
}
```

## .tres file updates

### pistol_settings.tres
- Add: `Damage=10.0`, `Speed=2000.0`, `Lifetime=3.0`, `ProjectileScene=StandardBullet.tscn`
- Remove: `DamageScale`, `Projectile` ref

### shotgun_settings.tres
- Add: `Damage=5.0`, `Speed=2000.0`, `Lifetime=3.0`, `ProjectileScene=StandardBullet.tscn`
- Remove: `DamageScale`, `Projectile` ref

### StandardBullet.tscn
- Set `_trailLength = 10` on root node
- Can potentially remove the Area2D CollisionShape2D since CCD handles hits via raycast

## Implementation order

1. **Create** `Scripts/Combat/Projectiles/ProjectileParams.cs`
2. **Update** `Projectile.cs` — CCD movement, visual exports, new Initialize, remove all hitscan trail logic and BodyEntered
3. **Update** `StandardBullet.cs` — simplify to just OnHit
4. **Update** `CombatEvents.cs` — remove `Projectile` field from HitEvent
5. **Update** `HazardSystem.cs` — remove `Projectile = null` from HitEvent
6. **Update** `WeaponDefinition.cs` — add Combat fields, ProjectileScene, remove old fields
7. **Update** `Weapon.cs` — build ProjectileParams, simple spawn
8. **Delete** `ProjectileDefinition.cs` and both `.tres` projectile files
9. **Update** weapon `.tres` files (pistol_settings.tres, shotgun_settings.tres)
10. **Update** `StandardBullet.tscn` — set trail export values

## Verification
- Build succeeds
- Pistol: bullets at speed 2000, damage 10, trail renders, hits enemies/walls accurately
- Shotgun: 5 spread pellets at speed 2000, damage 5 each
- Fast bullets (speed 100000): instant hit, no pass-through, acts like hitscan
- EffectsSystem hit effects still trigger
- No bullet-through-wall for any speed
