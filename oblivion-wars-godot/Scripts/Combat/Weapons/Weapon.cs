using Godot;

public partial class Weapon : Holdable
{
    [Export] private WeaponDefinition _weaponDefinition;
    [Export] private AnimationPlayer _animationPlayer;

    private bool _hasFiredThisPress = false;

    public override void _Ready()
    {
        if (_weaponDefinition != null)
            _useCooldown = _weaponDefinition.UseCooldown;
    }

    public void InitWeapon(WeaponDefinition definition)
    {
        _weaponDefinition = definition;
        _useCooldown = definition.UseCooldown;
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

        if (CameraController.Instance != null && _weaponDefinition.ScreenShake > 0)
            CameraController.Instance.Shake(_weaponDefinition.ScreenShake);
    }

    private void FireProjectile(Vector2 targetPosition, float damage, ProjectileDefinition projDef)
    {
        Vector2 baseDirection = (targetPosition - _owner.GlobalPosition).Normalized();

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

    private void SpawnProjectile(Vector2 direction, float damage, ProjectileDefinition projDef)
    {
        if (projDef.ProjectileScene == null) return;

        var projectile = projDef.ProjectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = _owner.GlobalPosition + _weaponDefinition.ProjectileSpawnOffset;
        projectile.Initialize(direction, damage, projDef, _owner);

        _owner.GetParent().AddChild(projectile);
    }

    private void FireInstant(Vector2 targetPosition, float damage, ProjectileDefinition projDef)
    {
        Vector2 direction = (targetPosition - _owner.GlobalPosition).Normalized();

        var spaceState = _owner.GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(
            _owner.GlobalPosition,
            _owner.GlobalPosition + direction * projDef.HitscanRange
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
            hitPosition = _owner.GlobalPosition + direction * projDef.HitscanRange;
        }

        // Spawn projectile scene for trail VFX
        if (projDef.ProjectileScene != null)
        {
            var trailProjectile = projDef.ProjectileScene.Instantiate<Projectile>();
            trailProjectile.GlobalPosition = _owner.GlobalPosition;
            trailProjectile.Initialize(direction, 0, projDef, _owner);
            trailProjectile.InitializeAsHitscanTrail(_owner.GlobalPosition, hitPosition);
            _owner.GetParent().AddChild(trailProjectile);
        }
    }
}
