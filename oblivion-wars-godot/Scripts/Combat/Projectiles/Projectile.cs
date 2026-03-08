using Godot;

public struct ProjectileParams
{
    public float Speed;
    public float Damage;
    public float Lifetime;
    public bool AffectedByGravity;
    public float GravityScale;
    public float ImpactForce;
    public bool EnableWallBounce;
    public bool EnableEnemyBounce;
    public int MaxBounces;
    public float CoefficientOfRestitution;
    public bool Explosive;
    public float ExplosionRadius;
    public float ExplosionDamageFalloff;
    public float TimedExplosion;
    public bool CancelTimedOnEnemyContact;
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
    private int _bounceCount = 0;
    private bool _stopped = false;

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
        // Stopped projectiles sit in place waiting for timed explosion
        if (_stopped)
            return;

        if (_params.AffectedByGravity)
            _velocity.Y += _gravity * _params.GravityScale * (float)delta;

        Vector2 displacement = _velocity * (float)delta;
        Vector2 nextPosition = GlobalPosition + displacement;

        // CCD: raycast from current to next position to catch collisions
        var spaceState = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, nextPosition);
        if (_shooter is CollisionObject2D col && GodotObject.IsInstanceValid(col))
            query.Exclude = new Godot.Collections.Array<Rid> { col.GetRid() };

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            var hitBody = (Node2D)result["collider"];
            var hitPosition = (Vector2)result["position"];
            var hitNormal = (Vector2)result["normal"];
            GlobalPosition = hitPosition;

            if (hitBody != _shooter && GodotObject.IsInstanceValid(hitBody) && !hitBody.IsQueuedForDeletion())
            {
                bool isEntity = hitBody is EntityCharacterBody2D;

                // CancelTimedOnEnemyContact takes priority over bouncing —
                // if set, hitting an entity always triggers immediate explosion
                if (isEntity && (_params.CancelTimedOnEnemyContact || _params.TimedExplosion <= 0f))
                {
                    OnHit(hitBody);
                    QueueFree();
                    return;
                }

                if (ShouldBounce(hitBody))
                {
                    Bounce(hitNormal);
                    return;
                }

                // Hitting a wall at max bounces: stop-and-wait if timed, otherwise immediate hit
                HandleTerminalHit(hitBody);
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

    /// <summary>Determines if the projectile should bounce off the hit body.</summary>
    private bool ShouldBounce(Node2D hitBody)
    {
        if (_bounceCount >= _params.MaxBounces)
            return false;

        bool isEntity = hitBody is EntityCharacterBody2D;

        if (isEntity)
            return _params.EnableEnemyBounce;

        return _params.EnableWallBounce;
    }

    /// <summary>Reflects velocity off the surface normal and increments bounce count.</summary>
    private void Bounce(Vector2 normal)
    {
        _velocity = _velocity.Bounce(normal) * _params.CoefficientOfRestitution;
        _bounceCount++;
        Rotation = _velocity.Angle();
        // Nudge away from the surface so the next frame's raycast doesn't start inside the collider
        GlobalPosition += normal * 1.0f;
    }

    /// <summary>Handles a terminal hit — either immediate OnHit or stop-and-wait for timed explosion.</summary>
    private void HandleTerminalHit(Node2D hitBody)
    {
        if (_params.TimedExplosion > 0f)
        {
            // Stop in place and wait for timed explosion
            _velocity = Vector2.Zero;
            _stopped = true;
        }
        else
        {
            OnHit(hitBody);
            QueueFree();
        }
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

        // Timed explosion: explode after the timer regardless of position
        if (_params.TimedExplosion > 0f)
        {
            if (_timeAlive >= _params.TimedExplosion)
            {
                OnHit(null);
                QueueFree();
            }
            return;
        }

        // Normal lifetime expiry
        if (_timeAlive >= _params.Lifetime)
        {
            QueueFree();
        }
    }

    protected virtual void OnHit(Node2D body)
    {
        if (_params.Explosive)
        {
            ApplySplashDamage(body);
            SpawnExplosionEffect();
        }
        else
        {
            if (body == null || !GodotObject.IsInstanceValid(body))
                return;

            EventBus.Instance.Raise(new HitEvent
            {
                TargetInstanceId = body.GetInstanceId(),
                SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
                BaseDamage = _params.Damage,
                ImpactForce = _params.ImpactForce,
                HitDirection = _velocity.Normalized(),
                HitPosition = GlobalPosition,
            });
        }
    }

    // Finds all bodies within the explosion radius and applies damage with linear falloff.
    // impactBody is the body the projectile physically collided with (always gets full damage).
    private void ApplySplashDamage(Node2D impactBody)
    {
        // Query a circle at the impact point to find all nearby bodies
        var spaceState = GetWorld2D().DirectSpaceState;
        var shape = new CircleShape2D();
        shape.Radius = _params.ExplosionRadius;

        var queryParams = new PhysicsShapeQueryParameters2D();
        queryParams.Shape = shape;
        queryParams.Transform = new Transform2D(0, GlobalPosition);
        queryParams.CollideWithBodies = true;
        queryParams.CollideWithAreas = false;

        var results = spaceState.IntersectShape(queryParams);
        ulong shooterId = _shooter?.GetInstanceId() ?? 0;

        // Determine the impact body's ID so we can force full damage on it
        ulong impactId = (impactBody != null && GodotObject.IsInstanceValid(impactBody))
            ? impactBody.GetInstanceId() : 0;

        // Collect unique entities hit. An entity with multiple colliders should only
        // take damage once, using the shortest distance to determine falloff.
        // The impact body always gets distance 0 (full damage, no falloff).
        var hitEntities = new System.Collections.Generic.Dictionary<ulong, (Node2D body, float distance)>();

        foreach (var result in results)
        {
            if (result["collider"].Obj is not Node2D hitBody) continue;
            if (!GodotObject.IsInstanceValid(hitBody) || hitBody.IsQueuedForDeletion()) continue;
            if (hitBody == _shooter) continue;

            ulong id = hitBody.GetInstanceId();

            // Impact body gets distance 0; others use actual distance for falloff
            float distance = (id == impactId) ? 0f : GlobalPosition.DistanceTo(hitBody.GlobalPosition);

            if (!hitEntities.TryGetValue(id, out var existing) || distance < existing.distance)
                hitEntities[id] = (hitBody, distance);
        }

        // Apply damage to each unique entity with linear falloff based on distance
        foreach (var (id, (hitBody, distance)) in hitEntities)
        {
            // t=0 at center (full damage), t=1 at edge (minimum damage)
            float t = Mathf.Clamp(distance / _params.ExplosionRadius, 0f, 1f);
            float damageMultiplier = Mathf.Lerp(1f, _params.ExplosionDamageFalloff, t);
            // Use the entity's center of mass (not origin at feet) for knockback direction,
            // so explosions at ground level push entities upward rather than purely sideways.
            Vector2 targetPoint = hitBody is EntityCharacterBody2D entity
                ? entity.GlobalCenterOfMass
                : hitBody.GlobalPosition;
            Vector2 hitDir = (targetPoint - GlobalPosition).Normalized();

            EventBus.Instance.Raise(new HitEvent
            {
                TargetInstanceId = id,
                SourceInstanceId = shooterId,
                BaseDamage = _params.Damage * damageMultiplier,
                ImpactForce = _params.ImpactForce * damageMultiplier,
                HitDirection = hitDir.LengthSquared() > 0 ? hitDir : _velocity.Normalized(),
                HitPosition = GlobalPosition,
            });
        }
    }

    private void SpawnExplosionEffect()
    {
        var effect = new ExplosionEffect();
        effect.Radius = _params.ExplosionRadius;
        effect.GlobalPosition = GlobalPosition;
        GetTree().Root.AddChild(effect);
    }
}
