using Godot;
using System;

/// <summary>
/// Weapon that spawns physical projectile objects (bullets, rockets, etc.)
/// </summary>
public partial class ProjectileWeapon : Weapon
{
	/// <summary>
	/// The projectile scene to instantiate when firing (e.g., bullet, rocket, arrow)
	/// </summary>
	[Export] private PackedScene _projectileScene;

	/// <summary>
	/// Offset from the weapon owner's position where projectiles spawn (typically in front of character)
	/// </summary>
	[Export] private Vector2 _projectileSpawnOffset = new Vector2(20, 0);

	/// <summary>
	/// Intensity of screen shake effect applied when firing (0 = no shake, higher = more shake)
	/// </summary>
	[Export] private float _screenShakeStrength = 1.5f;

	private CameraController _cameraController;

	public override void Initialize(Node2D owner)
	{
		base.Initialize(owner);

		// Find camera controller for screen shake
		_cameraController = owner.GetTree().Root.FindChild("CameraController", true, false) as CameraController;
	}

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

		// Apply screen shake
		if (_cameraController != null && _screenShakeStrength > 0)
		{
			_cameraController.Shake(_screenShakeStrength);
		}

		GD.Print("ProjectileWeapon fired!");
	}
}
