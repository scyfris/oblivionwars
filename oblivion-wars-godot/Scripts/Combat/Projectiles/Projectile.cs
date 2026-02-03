using Godot;

public abstract partial class Projectile : Area2D
{
    protected ProjectileDefinition _projectileDefinition;
    protected float _damage;
    protected Vector2 _direction;
    protected float _timeAlive = 0f;
    protected Node2D _shooter;

    public override void _Ready()
    {
        BodyEntered += _OnBodyEntered;
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

    public override void _PhysicsProcess(double delta)
    {
        UpdateMovement(delta);
        UpdateLifetime(delta);
    }

    protected abstract void UpdateMovement(double delta);

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
        QueueFree();
    }

    protected virtual void OnHit(Node2D body)
    {
    }
}
