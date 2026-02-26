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
    private float _hitscanTrailDuration = 0f;
    private float _hitscanTrailInitialWidth = 0f;
    private Vector2 _hitscanFrom;
    private Vector2 _hitscanTo;

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
        _hitscanTrailDuration = _projectileDefinition?.TrailDuration ?? 0.1f;
        _hitscanTrailTimer = _hitscanTrailDuration;

        // Disable collision for trail-only projectiles
        SetDeferred("monitoring", false);
        SetDeferred("monitorable", false);

        // Hide the projectile visual, only show trail
        var visual = GetNodeOrNull<Node2D>("Visual");
        if (visual != null)
            visual.Visible = false;

        if (_trail != null)
        {
            _hitscanFrom = from;
            _hitscanTo = to;
            _trail.ClearPoints();
            _trail.AddPoint(from);
            _trail.AddPoint(to);
            _trail.Visible = true;
            _hitscanTrailInitialWidth = _trail.Width;

            // Apply gradient from definition if set
            if (_projectileDefinition?.TrailGradient != null)
                _trail.Gradient = (Gradient)_projectileDefinition.TrailGradient.Duplicate();
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
        {
            QueueFree();
            return;
        }

        if (_trail == null || _hitscanTrailDuration <= 0f) return;

        float t = 1f - (_hitscanTrailTimer / _hitscanTrailDuration); // 0→1 over lifetime
        float widenScale = _projectileDefinition?.TrailWidenScale ?? 3f;
        bool useSweep = _projectileDefinition?.TrailUseSweep ?? true;

        if (useSweep)
        {
            // Sweep: move the gun-end point toward the hit point over time
            // The gradient remaps naturally over the shrinking line
            Vector2 newStart = _hitscanFrom.Lerp(_hitscanTo, t);
            _trail.SetPointPosition(0, newStart);
        }

        // Fade overall alpha over time
        float alpha = 1f - t;
        _trail.Modulate = new Color(1f, 1f, 1f, alpha);

        // Widen to simulate smoke dissipating
        _trail.Width = _hitscanTrailInitialWidth * (1f + t * widenScale);
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
        if (body == null || !GodotObject.IsInstanceValid(body) || body.IsQueuedForDeletion())
            return;

        if (body == _shooter)
            return;

        try
        {
            OnHit(body);
        }
        catch (System.ObjectDisposedException) { }

        QueueFree();
    }

    protected virtual void OnHit(Node2D body)
    {
    }
}
