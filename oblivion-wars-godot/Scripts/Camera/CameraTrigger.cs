using Godot;
using System;

/// <summary>
/// Trigger zone that directs camera attention when player enters.
/// Place this as an Area2D in your level to automatically control camera.
///
/// Use cases:
/// - Show important level elements (secrets, hazards, etc.)
/// - Direct player attention during gameplay
/// - Highlight objectives
/// </summary>
public partial class CameraTrigger : Area2D
{
	[Export] private CameraController _cameraController;
	[Export] private Vector2 _directionOffset = new Vector2(200, 0); // How much to offset camera
	[Export] private float _transitionSpeed = 2.0f;
	[Export] private bool _triggerOnce = true; // Only trigger the first time
	[Export] private float _resetDelay = 2.0f; // How long to show before returning to normal

	private bool _hasTriggered = false;
	private Timer _resetTimer;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		// Create reset timer
		_resetTimer = new Timer();
		_resetTimer.OneShot = true;
		_resetTimer.Timeout += OnResetTimeout;
		AddChild(_resetTimer);

		// Use singleton if not manually assigned
		if (_cameraController == null)
		{
			_cameraController = CameraController.Instance;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		// Check if it's the player
		if (body is not CharacterBody2D) return;
		if (_hasTriggered && _triggerOnce) return;

		// Direct camera attention
		if (_cameraController != null)
		{
			_cameraController.DirectAttention(_directionOffset, _transitionSpeed);
			_hasTriggered = true;

			// Start reset timer
			if (_resetDelay > 0)
			{
				_resetTimer.WaitTime = _resetDelay;
				_resetTimer.Start();
			}

			GD.Print($"CameraTrigger: Directing camera to offset {_directionOffset}");
		}
	}

	private void OnBodyExited(Node2D body)
	{
		// Optionally reset when player leaves
		// Currently handled by reset timer instead
	}

	private void OnResetTimeout()
	{
		// Return camera to normal follow
		if (_cameraController != null)
		{
			_cameraController.ResetDirectorOffset(_transitionSpeed);
		}
	}

	/// <summary>
	/// Reset the trigger so it can be activated again
	/// </summary>
	public void ResetTrigger()
	{
		_hasTriggered = false;
	}
}
