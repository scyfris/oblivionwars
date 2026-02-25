using Godot;

public partial class Weapon : Holdable
{
    [Export] private WeaponDefinition _weaponDefinition;
    [Export] private Node2D _projectileSpawnLocationNode;
    [Export] private AnimationPlayer _animationPlayer;

    private bool _hasFiredThisPress = false;

    private Vector2 AimDirection => GlobalTransform.X.Normalized();

    public override void UpdateAim(Vector2 targetPosition)
    {
        LookAt(targetPosition);

        // When under a parent with negative X scale (FlipRoot facing left),
        // the sprite appears flipped vertically. Correct by checking the
        // parent's transform determinant.
        var pt = GetParent<Node2D>().GlobalTransform;
        bool parentFlipped = (pt.X.X * pt.Y.Y - pt.X.Y * pt.Y.X) < 0;
        Scale = new Vector2(1, parentFlipped ? -1 : 1);
    }

    public override void _Ready()
    {
        if (_weaponDefinition != null)
            _useCooldown = _weaponDefinition.UseCooldown;
    }

    public override void OnUsePressed()
    {
        _hasFiredThisPress = false;
        TryFire();
    }

    public override void OnUseHeld()
    {
        if (!_weaponDefinition.IsAutomatic && _hasFiredThisPress) return;
        TryFire();
    }

    public override void OnUseReleased()
    {
        _hasFiredThisPress = false;
    }

    private void TryFire()
    {
        if (!CanUse() || _weaponDefinition?.Projectile == null) return;
        _hasFiredThisPress = true;

        var projDef = _weaponDefinition.Projectile;
        float damage = projDef.Damage * _weaponDefinition.DamageScale;

        if (projDef.Speed == 0)
            FireInstant(damage, projDef);
        else
            FireProjectile(damage, projDef);

        ResetCooldown();

        _animationPlayer?.Play("shoot");

        if (_owner is PlayerCharacterBody2D && CameraController.Instance != null && _weaponDefinition.ScreenShakeScale > 0)
            CameraController.Instance.Shake(_weaponDefinition.ScreenShakeScale, _weaponDefinition.ScreenShakeDurationScale);
    }

    private void FireProjectile(float damage, ProjectileDefinition projDef)
    {
        Vector2 baseDirection = AimDirection;

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
        return _projectileSpawnLocationNode != null ? _projectileSpawnLocationNode.GlobalPosition : _owner.GlobalPosition;
    }

    private void SpawnProjectile(Vector2 direction, float damage, ProjectileDefinition projDef)
    {
        if (projDef.ProjectileScene == null) return;

        var projectile = projDef.ProjectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GetSpawnPosition();
        projectile.Initialize(direction, damage, projDef, _owner);

        _owner.GetParent().AddChild(projectile);
    }

    private void FireInstant(float damage, ProjectileDefinition projDef)
    {
        Vector2 spawnPos = GetSpawnPosition();
        Vector2 direction = AimDirection;

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
