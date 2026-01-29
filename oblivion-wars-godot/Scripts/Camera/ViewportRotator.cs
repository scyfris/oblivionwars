using Godot;
using System;

/// <summary>
/// Rotates the SubViewportContainer to create camera rotation effect.
/// This works around the limitation that Camera2D rotation doesn't rotate the viewport.
/// </summary>
public partial class ViewportRotator : SubViewportContainer
{
	[Export] private Node2D _target; // The player to track rotation from
	[Export] private float _rotationSpeed = 5.0f; // How fast the viewport rotates
	[Export] private float _rotationDelay = 0.3f; // Delay before starting rotation

	private float _targetRotation = 0.0f;
	private float _delayTimer = 0.0f;
	private bool _isDelaying = false;

	public override void _Ready()
	{
		// Ensure container fills the screen and is anchored correctly
		SetAnchorsPreset(LayoutPreset.FullRect);
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;

		// Set pivot to center for rotation
		PivotOffset = Size / 2;

		// Get the SubViewport and set its size to match the window
		var viewport = GetNode<SubViewport>("GameViewport");
		if (viewport != null)
		{
			viewport.Size = (Vector2I)GetViewportRect().Size;
			GD.Print($"ViewportRotator: SubViewport sized to {viewport.Size}");
		}

		GD.Print("ViewportRotator: Ready");
	}

	public override void _Process(double delta)
	{
		if (_target == null) return;

		// Get target rotation from player's transform
		Vector2 targetUp = _target.Transform.Y.Normalized();
		float newTargetRotation = targetUp.Angle() - Mathf.Pi / 2;

		// Detect rotation change
		float angleDiff = Mathf.Abs(Mathf.AngleDifference(_targetRotation, newTargetRotation));
		if (angleDiff > 0.1f)
		{
			_targetRotation = newTargetRotation;
			_isDelaying = true;
			_delayTimer = _rotationDelay;
			GD.Print($"ViewportRotator: Rotation change detected, starting delay ({_rotationDelay}s)");
		}

		// Handle delay countdown
		if (_isDelaying)
		{
			_delayTimer -= (float)delta;
			if (_delayTimer <= 0)
			{
				_isDelaying = false;
				GD.Print("ViewportRotator: Delay expired, starting rotation");
			}
			else
			{
				return; // Still delaying
			}
		}

		// Smoothly rotate toward target
		Rotation = Mathf.LerpAngle(Rotation, _targetRotation, _rotationSpeed * (float)delta);
	}

	/// <summary>
	/// Set rotation immediately without delay (useful for initialization)
	/// </summary>
	public void SetRotationImmediate(float rotation)
	{
		Rotation = rotation;
		_targetRotation = rotation;
		_isDelaying = false;
	}
}
