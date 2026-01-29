using Godot;
using System;

/// <summary>
/// Base class for combat weapons. Extends Holdable with damage property.
/// Use() is implemented as Fire() internally for weapon-specific behavior.
/// </summary>
public abstract partial class Weapon : Holdable
{
	[Export] protected float _damage = 10.0f;

	public sealed override void Use(Vector2 targetPosition)
	{
		if (!CanUse()) return;
		Fire(targetPosition);
		ResetCooldown();
	}

	/// <summary>
	/// Weapon-specific firing logic. Override in subclasses (ProjectileWeapon, HitscanWeapon, etc.)
	/// </summary>
	protected abstract void Fire(Vector2 targetPosition);
}
