using Godot;
using System;

/// <summary>
/// Base class for all items that can be held and used by player or NPCs.
/// Decoupled from input - receives target position from any source (mouse, AI, gamepad).
/// </summary>
public abstract partial class Holdable : Node
{
	/// <summary>
	/// Minimum time in seconds between consecutive uses of this item (rate limiting)
	/// </summary>
	[Export] protected float _useCooldown = 0.2f;

	protected float _timeSinceLastUse = 999f;
	protected Node2D _owner; // Reference to entity holding this item (player or NPC)

	public virtual void Initialize(Node2D owner)
	{
		_owner = owner;
	}

	public virtual void Update(double delta)
	{
		_timeSinceLastUse += (float)delta;
	}

	public bool CanUse()
	{
		return _timeSinceLastUse >= _useCooldown;
	}

	/// <summary>
	/// Use this holdable targeting the given position.
	/// Target position can come from mouse (player), AI calculation (NPC), or any other source.
	/// </summary>
	public abstract void Use(Vector2 targetPosition);

	protected void ResetCooldown()
	{
		_timeSinceLastUse = 0f;
	}
}
