using Godot;
using System;

/// <summary>
/// Manages the currently active holdable item and switching between them.
/// Works with any entity (player or NPC) - input-agnostic.
/// </summary>
public partial class HoldableSystem : Node
{
	private Holdable[] _holdables; // Array of available holdables (weapons, tools, gadgets)
	private int _currentHoldableIndex = 0;
	private Holdable _currentHoldable;
	private Node2D _owner;

	public override void _Ready()
	{
		_owner = GetParent<Node2D>();

		// Get all Holdable children automatically
		var holdableList = new System.Collections.Generic.List<Holdable>();
		foreach (Node child in GetChildren())
		{
			if (child is Holdable holdable)
			{
				holdableList.Add(holdable);
			}
		}
		_holdables = holdableList.ToArray();

		// Initialize all holdables
		foreach (var holdable in _holdables)
		{
			holdable.Initialize(_owner);
		}

		if (_holdables.Length > 0)
		{
			_currentHoldable = _holdables[0];
			GD.Print($"HoldableSystem ready with {_holdables.Length} holdables. Starting with: {_currentHoldable.GetType().Name}");
		}
	}

	public void Update(double delta)
	{
		_currentHoldable?.Update(delta);
	}

	/// <summary>
	/// Use current holdable with given target position.
	/// Target can come from mouse (player), AI (NPC), or any other source.
	/// </summary>
	public void Use(Vector2 targetPosition)
	{
		if (_currentHoldable != null && _currentHoldable.CanUse())
		{
			_currentHoldable.Use(targetPosition);
		}
	}

	public void SwitchHoldable(int index)
	{
		if (_holdables != null && index >= 0 && index < _holdables.Length)
		{
			_currentHoldableIndex = index;
			_currentHoldable = _holdables[index];
			GD.Print($"Switched to holdable: {_currentHoldable.GetType().Name}");
		}
	}

	public void NextHoldable()
	{
		if (_holdables != null && _holdables.Length > 0)
		{
			_currentHoldableIndex = (_currentHoldableIndex + 1) % _holdables.Length;
			_currentHoldable = _holdables[_currentHoldableIndex];
			GD.Print($"Switched to holdable: {_currentHoldable.GetType().Name}");
		}
	}
}
