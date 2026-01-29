using Godot;
using System;

public partial class StandardBullet : Projectile
{
	protected override void UpdateMovement(double delta)
	{
		// Linear movement
		Position += _direction * _speed * (float)delta;
	}

	protected override void OnHit(Node2D body)
	{
		// Future: Hit effects, particles, etc.
		GD.Print($"StandardBullet hit: {body.Name}");
	}
}
