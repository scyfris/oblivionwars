using Godot;

public abstract partial class Projectile : Area2D
{
    [Export] protected Line2D _trail;

    protected ProjectileDefinition _projectileDefinition;
    protected float _damage;
    protected Vector2 _direction;
    protected float _timeAlive = 0f;
    protected Node2D _shooter;

    private bool _isHitscanTrail = false;
    private float _hitscanTrailTimer = 0f;

    public override void _Ready()
    {
        BodyEntered += _OnBodyEntered;

        if (_trail != null)
            _trail.TopLevel = true;
    }

    public virtual void Initialize(Vector2 direction, float damage,
        ProjectileDefinition definition, Node2D shooter = null)
    {
        _direction = direction.Normalized();
        _damage = damage;
        _projectileDefinition = definition;
        _shooter = shooter;
        Rotation = direction.Angle();
    }

    public void InitializeAsHitscanTrail(Vector2 from, Vector2 to)
    {
        _isHitscanTrail = true;
        _hitscanTrailTimer = _projectileDefinition?.TrailDuration ?? 0.1f;

        // Disable collision for trail-only projectiles
        SetDeferred("monitoring", false);
        SetDeferred("monitorable", false);

        // Hide the projectile visual, only show trail
        var visual = GetNodeOrNull<Node2D>("Visual");
        if (visual != null)
            visual.Visible = false;

        if (_trail != null)
        {
            _trail.ClearPoints();
            _trail.AddPoint(from);
            _trail.AddPoint(to);
            _trail.Visible = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isHitscanTrail)
        {
            UpdateHitscanTrail(delta);
            return;
        }

        UpdateMovement(delta);
        UpdateTrail();
        UpdateLifetime(delta);
    }

    protected abstract void UpdateMovement(double delta);

    private void UpdateTrail()
    {
        if (_trail == null || _isHitscanTrail) return;

        int maxPoints = _projectileDefinition?.TrailLength ?? 10;
        _trail.AddPoint(GlobalPosition);

        while (_trail.GetPointCount() > maxPoints)
            _trail.RemovePoint(0);

        _trail.Visible = _trail.GetPointCount() > 1;
    }

    private void UpdateHitscanTrail(double delta)
    {
        _hitscanTrailTimer -= (float)delta;
        if (_hitscanTrailTimer <= 0)
            QueueFree();
    }

    protected virtual void UpdateLifetime(double delta)
    {
        _timeAlive += (float)delta;
        float lifetime = _projectileDefinition?.Lifetime ?? 3.0f;
        if (_timeAlive >= lifetime)
        {
            QueueFree();
        }
    }

    protected virtual void _OnBodyEntered(Node2D body)
    {
        if (body == _shooter)
            return;

        OnHit(body);
        SpawnHitEffect();
        QueueFree();
    }

    protected void SpawnHitEffect()
    {
        if (_projectileDefinition?.HitEffect == null) return;

        var effect = _projectileDefinition.HitEffect.Instantiate<Node2D>();
        effect.GlobalPosition = GlobalPosition;
        effect.Rotation = _direction.Angle() + Mathf.Pi;
        GetTree().Root.AddChild(effect);
    }

    protected virtual void OnHit(Node2D body)
    {
    }
}
