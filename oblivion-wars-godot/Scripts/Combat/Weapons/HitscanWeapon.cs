using Godot;
using System;

/// <summary>
/// Instant-hit raycast weapon (pistol, sniper, laser, etc.)
/// No physical projectile - damage is instant along raycast line.
/// </summary>
public partial class HitscanWeapon : Weapon
{
	[Export] private float _range = 1000.0f;
	[Export] private float _trailDuration = 0.1f;

	private Line2D _bulletTrail; // Visual trail
	private float _trailTimer = 0f;

	public override void Initialize(Node2D owner)
	{
		base.Initialize(owner);

		// Get the Line2D child
		_bulletTrail = GetNodeOrNull<Line2D>("BulletTrail");
		if (_bulletTrail == null)
		{
			GD.PrintErr("HitscanWeapon: BulletTrail child not found!");
		}
		else
		{
			GD.Print("HitscanWeapon: BulletTrail found and ready");
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

	protected override void Fire(Vector2 targetPosition)
	{
		// Calculate direction from owner to target
		Vector2 direction = (targetPosition - _owner.GlobalPosition).Normalized();

		// Perform raycast from owner position toward target
		var spaceState = _owner.GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(
			_owner.GlobalPosition,
			_owner.GlobalPosition + direction * _range
		);

		// Exclude the shooter from the raycast so we don't hit ourselves
		if (_owner is CollisionObject2D collisionOwner)
		{
			query.Exclude = new Godot.Collections.Array<Rid> { collisionOwner.GetRid() };
		}

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			// Hit something
			var hitPosition = (Vector2)result["position"];
			var hitBody = (Node2D)result["collider"];

			// Future: Apply damage to hitBody if it has health component
			GD.Print($"HitscanWeapon hit: {hitBody.Name} at {hitPosition}");

			// Show bullet trail to hit point
			ShowBulletTrail(_owner.GlobalPosition, hitPosition);
		}
		else
		{
			// Missed - show trail to max range
			ShowBulletTrail(_owner.GlobalPosition, _owner.GlobalPosition + direction * _range);
		}

		GD.Print("HitscanWeapon fired!");
	}

	private void ShowBulletTrail(Vector2 from, Vector2 to)
	{
		if (_bulletTrail != null)
		{
			// Line2D needs to be in global canvas coordinates
			// Since it's a direct child of HitscanWeapon (a Node), we use global positions directly
			_bulletTrail.ClearPoints();
			_bulletTrail.AddPoint(from);
			_bulletTrail.AddPoint(to);
			_bulletTrail.Visible = true;
			_trailTimer = _trailDuration;

			GD.Print($"BulletTrail drawn from {from} to {to}");
		}
		else
		{
			GD.Print("BulletTrail is null, cannot draw line");
		}
	}
}
