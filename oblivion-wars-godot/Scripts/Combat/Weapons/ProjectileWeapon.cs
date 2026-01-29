using Godot;
using System;

/// <summary>
/// Weapon that spawns physical projectile objects (bullets, rockets, etc.)
/// </summary>
public partial class ProjectileWeapon : Weapon
{
	[Export] private PackedScene _projectileScene;
	[Export] private Vector2 _projectileSpawnOffset = new Vector2(20, 0);

	protected override void Fire(Vector2 targetPosition)
	{
		// Calculate direction from owner to target
		Vector2 direction = (targetPosition - _owner.GlobalPosition).Normalized();

		// Spawn projectile
		var projectile = _projectileScene.Instantiate<Projectile>();
		projectile.GlobalPosition = _owner.GlobalPosition + _projectileSpawnOffset;
		projectile.Initialize(direction, _damage, _owner); // Pass shooter so bullet ignores them

		// Add to scene tree (as sibling of owner, not child)
		_owner.GetParent().AddChild(projectile);

		GD.Print("ProjectileWeapon fired!");
	}
}
