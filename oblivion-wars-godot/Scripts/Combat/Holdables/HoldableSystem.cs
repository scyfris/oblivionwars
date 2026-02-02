using Godot;
using System;

/// <summary>
/// Manages left and right holdable slots for an entity.
/// Instantiates holdable scenes, positions them, and routes Use() calls.
/// Works with any entity (player or NPC) - input-agnostic.
/// </summary>
public partial class HoldableSystem : Node
{
	[Export] private PackedScene _leftHoldableScene;
	[Export] private PackedScene _rightHoldableScene;

	private Holdable _leftHoldable;
	private Holdable _rightHoldable;
	private Node2D _owner;

	public void Initialize(Node2D owner)
	{
		_owner = owner;

		if (_leftHoldableScene != null)
		{
			_leftHoldable = InstantiateHoldable(_leftHoldableScene);
		}

		if (_rightHoldableScene != null)
		{
			_rightHoldable = InstantiateHoldable(_rightHoldableScene);
		}
	}

	public void Update(double delta)
	{
		_leftHoldable?.Update(delta);
		_rightHoldable?.Update(delta);
	}

	public void UseLeft(Vector2 targetPosition)
	{
		_leftHoldable?.Use(targetPosition);
	}

	public void UseRight(Vector2 targetPosition)
	{
		_rightHoldable?.Use(targetPosition);
	}

	public void SwapLeft(PackedScene newScene)
	{
		if (_leftHoldable != null)
		{
			_leftHoldable.OnUnequip();
			_leftHoldable.QueueFree();
			_leftHoldable = null;
		}

		if (newScene != null)
		{
			_leftHoldableScene = newScene;
			_leftHoldable = InstantiateHoldable(newScene);
		}
	}

	public void SwapRight(PackedScene newScene)
	{
		if (_rightHoldable != null)
		{
			_rightHoldable.OnUnequip();
			_rightHoldable.QueueFree();
			_rightHoldable = null;
		}

		if (newScene != null)
		{
			_rightHoldableScene = newScene;
			_rightHoldable = InstantiateHoldable(newScene);
		}
	}

	private Holdable InstantiateHoldable(PackedScene scene)
	{
		var instance = scene.Instantiate<Holdable>();
		AddChild(instance);
		instance.InitOwner(_owner);
		instance.OnEquip();
		return instance;
	}
}
