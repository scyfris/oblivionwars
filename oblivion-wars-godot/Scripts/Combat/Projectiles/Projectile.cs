using Godot;

public struct ProjectileParams
{
    public float Speed;
    public float Damage;
    public float Lifetime;
    public bool AffectedByGravity;
    public float GravityScale;
}

public partial class Projectile : Area2D
{
    [Export] protected Line2D _trail;

    [ExportGroup("Trail")]
    [Export] protected int _trailLength = 10;

    protected ProjectileParams _params;
    protected Vector2 _velocity;
    protected float _timeAlive = 0f;
    protected Node2D _shooter;

    private float _gravity;

    public override void _Ready()
    {
        if (_trail != null)
            _trail.TopLevel = true;

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
    }

    public virtual void Initialize(Vector2 direction, ProjectileParams projectileParams, Node2D shooter = null)
    {
        _velocity = direction.Normalized() * projectileParams.Speed;
        _params = projectileParams;
        _shooter = shooter;
        Rotation = direction.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateMovement(delta);
        UpdateTrail();
        UpdateLifetime(delta);
    }

    protected virtual void UpdateMovement(double delta)
    {
        if (_params.AffectedByGravity)
            _velocity.Y += _gravity * _params.GravityScale * (float)delta;

        Vector2 displacement = _velocity * (float)delta;
        Vector2 nextPosition = GlobalPosition + displacement;

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

        if (_params.AffectedByGravity)
            Rotation = _velocity.Angle();
    }

    private void UpdateTrail()
    {
        if (_trail == null) return;

        _trail.AddPoint(GlobalPosition);

        while (_trail.GetPointCount() > _trailLength)
            _trail.RemovePoint(0);

        _trail.Visible = _trail.GetPointCount() > 1;
    }

    protected virtual void UpdateLifetime(double delta)
    {
        _timeAlive += (float)delta;
        if (_timeAlive >= _params.Lifetime)
        {
            QueueFree();
        }
    }

    protected virtual void OnHit(Node2D body)
    {
        if (body == null || !GodotObject.IsInstanceValid(body))
            return;

        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = body.GetInstanceId(),
            SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
            BaseDamage = _params.Damage,
            HitDirection = _velocity.Normalized(),
            HitPosition = GlobalPosition,
        });
    }
}
