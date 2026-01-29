using Godot;
using System;

public abstract partial class Projectile : Area2D
{
	[Export] protected float _speed = 800.0f;
	[Export] protected float _lifetime = 3.0f;
	[Export] protected float _damage = 10.0f;

	protected Vector2 _direction;
	protected float _timeAlive = 0f;
	protected Node2D _shooter; // The entity that fired this projectile

	public override void _Ready()
	{
		BodyEntered += _OnBodyEntered;
	}

	public virtual void Initialize(Vector2 direction, float damage, Node2D shooter = null)
	{
		_direction = direction.Normalized();
		_damage = damage;
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
		if (_timeAlive >= _lifetime)
		{
			QueueFree();
		}
	}

	protected virtual void _OnBodyEntered(Node2D body)
	{
		// Ignore collision with the shooter
		if (body == _shooter)
		{
			return;
		}

		// Future: Apply damage to body if it has health component
		OnHit(body);
		QueueFree();
	}

	protected virtual void OnHit(Node2D body)
	{
		// Override in subclasses for hit effects, damage application, etc.
	}
}
