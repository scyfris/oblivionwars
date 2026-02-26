using Godot;

public partial class StandardBullet : Projectile
{
    public override void Initialize(Vector2 direction, float damage,
        ProjectileDefinition definition, Node2D shooter = null)
    {
        base.Initialize(direction, damage, definition, shooter);

        if (definition.Speed == 0 && shooter != null)
            PerformHitscan();
    }

    protected override void UpdateMovement(double delta)
    {
        float speed = _projectileDefinition?.Speed ?? 800.0f;
        GlobalPosition += _direction * speed * (float)delta;
    }

    protected override void OnHit(Node2D body)
    {
        if (body == null || !GodotObject.IsInstanceValid(body))
            return;

        RaiseHitEvent(body, GlobalPosition);
    }

    private void PerformHitscan()
    {
        var projDef = _projectileDefinition;
        Vector2 from = GlobalPosition;
        Vector2 to = from + _direction * projDef.HitscanRange;

        var spaceState = _shooter.GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(from, to);

        if (_shooter is CollisionObject2D collisionOwner)
            query.Exclude = new Godot.Collections.Array<Rid> { collisionOwner.GetRid() };

        var result = spaceState.IntersectRay(query);

        Vector2 hitPosition;
        if (result.Count > 0)
        {
            hitPosition = (Vector2)result["position"];
            var hitBody = (Node2D)result["collider"];
            RaiseHitEvent(hitBody, hitPosition);
        }
        else
        {
            hitPosition = to;
        }

        InitializeAsHitscanTrail(from, hitPosition);
    }

    private void RaiseHitEvent(Node2D target, Vector2 hitPosition)
    {
        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = target.GetInstanceId(),
            SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
            BaseDamage = _damage,
            HitDirection = _direction,
            HitPosition = hitPosition,
            Projectile = _projectileDefinition
        });
    }
}
