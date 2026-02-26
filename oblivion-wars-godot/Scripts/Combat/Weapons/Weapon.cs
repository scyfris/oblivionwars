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

    protected override float GetUseCooldown() => _weaponDefinition?.UseCooldown ?? 0.2f;

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
        };

        projectile.Initialize(direction, projectileParams, _owner);
        _owner.GetParent().AddChild(projectile);
    }
}
