using Godot;

public partial class Weapon : Holdable
{
    private WeaponDefinition _weaponDefinition;
    [Export] private Node2D _projectileSpawnLocationNode;
    [Export] private AnimationPlayer _animationPlayer;

    public void SetDefinition(WeaponDefinition definition)
    {
        _weaponDefinition = definition;
    }

    private bool _hasFiredThisPress = false;
    private float _currentRecoilDeg = 0f;
    private float _timeSinceLastShot = 0f;
    private bool _isFiring = false;

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

    protected override float GetUseCooldown() => _weaponDefinition?.FireRate ?? 0.2f;

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!_isFiring && _currentRecoilDeg > 0f && _weaponDefinition != null)
        {
            _timeSinceLastShot += (float)delta;
            if (_weaponDefinition.RecoilRecoveryTime > 0f)
            {
                float t = Mathf.Clamp(_timeSinceLastShot / _weaponDefinition.RecoilRecoveryTime, 0f, 1f);
                _currentRecoilDeg = Mathf.Lerp(_currentRecoilDeg, 0f, t);
                if (_currentRecoilDeg < 0.01f)
                    _currentRecoilDeg = 0f;
            }
            else
            {
                _currentRecoilDeg = 0f;
            }
        }
    }

    /// <summary>Current recoil bloom in degrees (without base variability).</summary>
    public float CurrentRecoilDeg => _currentRecoilDeg;
    /// <summary>Maximum recoil bloom cap from the weapon definition.</summary>
    public float MaxRecoilDeg => _weaponDefinition?.MaxRecoilDeg ?? 0f;
    /// <summary>Current per-bullet variability in degrees (base + recoil bloom).</summary>
    public float CurrentSpreadDeg => (_weaponDefinition?.VariabilitySpreadPerBulletDeg ?? 0f) + _currentRecoilDeg;

    /// <summary>Total angular radius in degrees from aim center to the outermost possible bullet.
    /// For multi-pellet weapons this includes half the fan angle plus per-bullet variability.
    /// This is what the crosshair circle should represent.</summary>
    public float TotalSpreadRadiusDeg
    {
        get
        {
            if (_weaponDefinition == null) return 0f;
            float halfFan = _weaponDefinition.SpreadCount > 1 ? _weaponDefinition.SpreadAngleDeg / 2f : 0f;
            return halfFan + CurrentSpreadDeg;
        }
    }

    public override void OnUsePressed()
    {
        _hasFiredThisPress = false;
        _isFiring = true;
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
        _isFiring = false;
    }

    private void TryFire()
    {
        if (!CanUse() || _weaponDefinition?.ProjectileScene == null) return;
        _hasFiredThisPress = true;

        FireProjectile();

        _currentRecoilDeg = Mathf.Min(_currentRecoilDeg + _weaponDefinition.RecoilPerShotDeg, _weaponDefinition.MaxRecoilDeg);
        _timeSinceLastShot = 0f;

        ResetCooldown();

        _animationPlayer?.Play("shoot");

        if (_owner is PlayerCharacterBody2D && CameraController.Instance != null && _weaponDefinition.ScreenShakeScale > 0)
            CameraController.Instance.Shake(_weaponDefinition.ScreenShakeScale, _weaponDefinition.ScreenShakeDurationScale);
    }

    private float GetRandomSpreadRad()
    {
        float spreadDeg = CurrentSpreadDeg;
        if (spreadDeg <= 0f) return 0f;
        return Mathf.DegToRad((float)GD.RandRange(-spreadDeg, spreadDeg));
    }

    private void FireProjectile()
    {
        Vector2 baseDirection = AimDirection;

        if (_weaponDefinition.SpreadCount <= 1)
        {
            SpawnProjectile(baseDirection.Rotated(GetRandomSpreadRad()));
        }
        else
        {
            float totalAngle = Mathf.DegToRad(_weaponDefinition.SpreadAngleDeg);
            float startAngle = -totalAngle / 2f;
            float step = _weaponDefinition.SpreadCount > 1
                ? totalAngle / (_weaponDefinition.SpreadCount - 1)
                : 0f;

            for (int i = 0; i < _weaponDefinition.SpreadCount; i++)
            {
                float angle = startAngle + step * i + GetRandomSpreadRad();
                SpawnProjectile(baseDirection.Rotated(angle));
            }
        }
    }

    private Vector2 GetSpawnPosition()
    {
        return _projectileSpawnLocationNode != null ? _projectileSpawnLocationNode.GlobalPosition : _owner.GlobalPosition;
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
            AffectedByGravity = _weaponDefinition.ProjectileAffectedByGravity,
            GravityScale = _weaponDefinition.ProjectileGravityScale,
            ImpactForce = _weaponDefinition.ImpactForce,
            EnableWallBounce = _weaponDefinition.EnableWallBounce,
            EnableEnemyBounce = _weaponDefinition.EnableEnemyBounce,
            MaxBounces = _weaponDefinition.MaxBounces,
            CoefficientOfRestitution = _weaponDefinition.CoefficientOfRestitution,
            Explosive = _weaponDefinition.Explosive,
            ExplosionRadius = _weaponDefinition.ExplosionRadius,
            ExplosionDamageFalloff = _weaponDefinition.ExplosionDamageFalloff,
            TimedExplosion = _weaponDefinition.TimedExplosion,
            CancelTimedOnEnemyContact = _weaponDefinition.CancelTimedOnEnemyContact,
        };

        projectile.Initialize(direction, projectileParams, _owner);
        _owner.GetParent().AddChild(projectile);
    }
}
