using Godot;

public partial class Weapon : Holdable
{
    [Export] private WeaponDefinition _weaponDefinition;
    [Export] private Node2D _projectileSpawn;
    [Export] private AnimationPlayer _animationPlayer;

    private bool _hasFiredThisPress = false;

    public override void _Ready()
    {
        if (_weaponDefinition != null)
            _useCooldown = _weaponDefinition.UseCooldown;
    }

    public override void OnUsePressed(Vector2 targetPosition)
    {
        _hasFiredThisPress = false;
        TryFire(targetPosition);
    }

    public override void OnUseHeld(Vector2 targetPosition)
    {
        if (!_weaponDefinition.IsAutomatic && _hasFiredThisPress) return;
        TryFire(targetPosition);
    }

    public override void OnUseReleased(Vector2 targetPosition)
    {
        _hasFiredThisPress = false;
    }

    private void TryFire(Vector2 targetPosition)
    {
        if (!CanUse() || _weaponDefinition?.Projectile == null) return;
        _hasFiredThisPress = true;

        var projDef = _weaponDefinition.Projectile;
        float damage = projDef.Damage * _weaponDefinition.DamageScale;

        if (projDef.Speed == 0)
            FireInstant(targetPosition, damage, projDef);
        else
            FireProjectile(targetPosition, damage, projDef);

        ResetCooldown();

        _animationPlayer?.Play("shoot");

        if (CameraController.Instance != null && _weaponDefinition.ScreenShakeScale > 0)
            CameraController.Instance.Shake(_weaponDefinition.ScreenShakeScale, _weaponDefinition.ScreenShakeDurationScale);
    }

    private void FireProjectile(Vector2 targetPosition, float damage, ProjectileDefinition projDef)
    {
        Vector2 baseDirection = (targetPosition - GetSpawnPosition()).Normalized();

        if (_weaponDefinition.SpreadCount <= 1)
        {
            SpawnProjectile(baseDirection, damage, projDef);
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
                Vector2 spreadDir = baseDirection.Rotated(angle);
                SpawnProjectile(spreadDir, damage, projDef);
            }
        }
    }

    private Vector2 GetSpawnPosition()
    {
        return _projectileSpawn != null ? _projectileSpawn.GlobalPosition : _owner.GlobalPosition;
    }

    private void SpawnProjectile(Vector2 direction, float damage, ProjectileDefinition projDef)
    {
        if (projDef.ProjectileScene == null) return;

        var projectile = projDef.ProjectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GetSpawnPosition();
        projectile.Initialize(direction, damage, projDef, _owner);

        _owner.GetParent().AddChild(projectile);
    }

    private void FireInstant(Vector2 targetPosition, float damage, ProjectileDefinition projDef)
    {
        Vector2 spawnPos = GetSpawnPosition();
        Vector2 direction = (targetPosition - spawnPos).Normalized();

        var spaceState = _owner.GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(
            spawnPos,
            spawnPos + direction * projDef.HitscanRange
        );

        if (_owner is CollisionObject2D collisionOwner)
        {
            query.Exclude = new Godot.Collections.Array<Rid> { collisionOwner.GetRid() };
        }

        var result = spaceState.IntersectRay(query);

        Vector2 hitPosition;
        if (result.Count > 0)
        {
            hitPosition = (Vector2)result["position"];
            var hitBody = (Node2D)result["collider"];

            EventBus.Instance.Raise(new HitEvent
            {
                TargetInstanceId = hitBody.GetInstanceId(),
                SourceInstanceId = _owner.GetInstanceId(),
                BaseDamage = damage,
                HitDirection = direction,
                HitPosition = hitPosition,
                Projectile = projDef
            });
        }
        else
        {
            hitPosition = spawnPos + direction * projDef.HitscanRange;
        }

        // Spawn hit effect at impact point
        if (result.Count > 0 && projDef.HitEffect != null)
        {
            var effect = projDef.HitEffect.Instantiate<Node2D>();
            effect.GlobalPosition = hitPosition;
            effect.Rotation = direction.Angle() + Mathf.Pi;
            _owner.GetTree().Root.AddChild(effect);
        }

        // Spawn projectile scene for trail VFX — AddChild first so _trail export resolves
        if (projDef.ProjectileScene != null)
        {
            var trailProjectile = projDef.ProjectileScene.Instantiate<Projectile>();
            trailProjectile.GlobalPosition = spawnPos;
            trailProjectile.Initialize(direction, 0, projDef, _owner);
            _owner.GetParent().AddChild(trailProjectile);
            trailProjectile.InitializeAsHitscanTrail(spawnPos, hitPosition);
        }
    }
}
