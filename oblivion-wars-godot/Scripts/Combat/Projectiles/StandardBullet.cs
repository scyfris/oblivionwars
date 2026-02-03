using Godot;

public partial class StandardBullet : Projectile
{
    protected override void UpdateMovement(double delta)
    {
        float speed = _projectileDefinition?.Speed ?? 800.0f;
        Position += _direction * speed * (float)delta;
    }

    protected override void OnHit(Node2D body)
    {
        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = body.GetInstanceId(),
            SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
            BaseDamage = _damage,
            HitDirection = _direction,
            HitPosition = GlobalPosition,
            Projectile = _projectileDefinition
        });
    }
}
