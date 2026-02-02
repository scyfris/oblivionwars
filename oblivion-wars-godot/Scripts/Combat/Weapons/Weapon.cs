using Godot;
using System;

/// <summary>
/// Data-driven weapon class. Handles both projectile and hitscan weapons
/// based on WeaponDefinition data. No per-weapon subclasses needed.
/// </summary>
public partial class Weapon : Holdable
{
	[Export] protected WeaponDefinition _weaponDefinition;
	[Export] private Line2D _bulletTrail;
	[Export] private AnimationPlayer _animationPlayer;

	private float _trailTimer = 0f;

	public override void _Ready()
	{
		if (_weaponDefinition != null)
		{
			_useCooldown = _weaponDefinition.UseCooldown;
		}
	}

	public override void Update(double delta)
	{
		base.Update(delta);

		// Fade out bullet trail
		if (_bulletTrail != null && _trailTimer > 0)
		{
			_trailTimer -= (float)delta;
			if (_trailTimer <= 0)
			{
				_bulletTrail.Visible = false;
			}
		}
	}

	public override void Use(Vector2 targetPosition)
	{
		if (!CanUse()) return;

		if (_weaponDefinition.ProjectileScene != null)
			FireProjectile(targetPosition);
		else
			FireHitscan(targetPosition);

		ResetCooldown();

		// Trigger animation if present
		_animationPlayer?.Play("shoot");

		// Apply screen shake
		if (CameraController.Instance != null && _weaponDefinition.ScreenShake > 0)
		{
			CameraController.Instance.Shake(_weaponDefinition.ScreenShake);
		}
	}

	private void FireProjectile(Vector2 targetPosition)
	{
		Vector2 baseDirection = (targetPosition - _owner.GlobalPosition).Normalized();

		if (_weaponDefinition.SpreadCount <= 1)
		{
			SpawnProjectile(baseDirection);
		}
		else
		{
			// Spread pattern: distribute projectiles evenly across the spread angle
			float totalAngle = Mathf.DegToRad(_weaponDefinition.SpreadAngle);
			float startAngle = -totalAngle / 2f;
			float step = _weaponDefinition.SpreadCount > 1
				? totalAngle / (_weaponDefinition.SpreadCount - 1)
				: 0f;

			for (int i = 0; i < _weaponDefinition.SpreadCount; i++)
			{
				float angle = startAngle + step * i;
				Vector2 spreadDir = baseDirection.Rotated(angle);
				SpawnProjectile(spreadDir);
			}
		}

		GD.Print($"Weapon fired {_weaponDefinition.SpreadCount} projectile(s)!");
	}

	private void SpawnProjectile(Vector2 direction)
	{
		var projectile = _weaponDefinition.ProjectileScene.Instantiate<Projectile>();
		projectile.GlobalPosition = _owner.GlobalPosition + _weaponDefinition.ProjectileSpawnOffset;
		projectile.Initialize(direction, _weaponDefinition.Damage, _owner);

		// Add to scene tree (as sibling of owner, not child)
		_owner.GetParent().AddChild(projectile);
	}

	private void FireHitscan(Vector2 targetPosition)
	{
		Vector2 direction = (targetPosition - _owner.GlobalPosition).Normalized();

		var spaceState = _owner.GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(
			_owner.GlobalPosition,
			_owner.GlobalPosition + direction * _weaponDefinition.HitscanRange
		);

		// Exclude the shooter from the raycast
		if (_owner is CollisionObject2D collisionOwner)
		{
			query.Exclude = new Godot.Collections.Array<Rid> { collisionOwner.GetRid() };
		}

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			var hitPosition = (Vector2)result["position"];
			var hitBody = (Node2D)result["collider"];

			GD.Print($"Weapon hitscan hit: {hitBody.Name} at {hitPosition}");
			ShowBulletTrail(_owner.GlobalPosition, hitPosition);
		}
		else
		{
			ShowBulletTrail(_owner.GlobalPosition, _owner.GlobalPosition + direction * _weaponDefinition.HitscanRange);
		}

		GD.Print("Weapon fired hitscan!");
	}

	private void ShowBulletTrail(Vector2 from, Vector2 to)
	{
		if (_bulletTrail == null) return;

		_bulletTrail.ClearPoints();
		_bulletTrail.AddPoint(from);
		_bulletTrail.AddPoint(to);
		_bulletTrail.Visible = true;
		_trailTimer = _weaponDefinition.TrailDuration;
	}
}
