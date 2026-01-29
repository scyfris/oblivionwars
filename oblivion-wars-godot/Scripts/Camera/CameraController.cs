using Godot;
using System;

/// <summary>
/// Advanced platformer camera system with smooth following, rotation support,
/// directorial control, and extensible effects.
///
/// Features:
/// - Smooth spring-based following (player not always centered)
/// - Rotates to match player's up direction
/// - Camera offset/directing for showing level elements
/// - Screen shake support
/// - Cutscene mode
/// - Extensible for full-screen effects
/// </summary>
public partial class CameraController : Node
{
	[ExportGroup("Target")]
	[Export] private Node2D _target; // The player character to follow

	[ExportGroup("Follow Settings")]
	[Export] private float _followSpeed = 5.0f; // How quickly camera catches up to target
	[Export] private Vector2 _followOffset = Vector2.Zero; // Offset from target position
	[Export] private float _lookAheadDistance = 50.0f; // How far ahead to look based on movement
	[Export] private float _lookAheadSpeed = 2.0f; // How quickly look-ahead adjusts

	[ExportGroup("Rotation Settings")]
	[Export] private bool _rotateWithTarget = true; // Match target's up direction
	[Export] private float _rotationSpeed = 3.0f; // How quickly camera rotates to match

	[ExportGroup("Boundaries")]
	[Export] private bool _useBoundaries = false;
	[Export] private Rect2 _cameraBounds = new Rect2(-10000, -10000, 20000, 20000);

	[ExportGroup("Screen Shake")]
	[Export] private float _shakeDecayRate = 3.0f; // How quickly shake fades

	private Camera2D _camera;
	private Vector2 _velocity = Vector2.Zero;
	private Vector2 _lookAheadOffset = Vector2.Zero;
	private Vector2 _currentOffset = Vector2.Zero;

	// Screen shake
	private float _shakeStrength = 0.0f;
	private float _shakeFrequency = 20.0f;
	private float _shakeTime = 0.0f;

	// Camera directing/offset (for showing level elements, cutscenes)
	private Vector2 _directorOffset = Vector2.Zero;
	private float _directorOffsetSpeed = 2.0f;
	private Vector2 _targetDirectorOffset = Vector2.Zero;

	// Camera mode
	private CameraMode _mode = CameraMode.Follow;
	private Vector2 _fixedPosition = Vector2.Zero;
	private float _fixedRotation = 0.0f;

	public enum CameraMode
	{
		Follow,      // Standard following with spring
		Fixed,       // Camera locked to specific position/rotation
		Cutscene,    // Camera controlled by external script
		Directed     // Temporarily offset to show something
	}

	public override void _Ready()
	{
		// Get or create Camera2D child
		_camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (_camera == null)
		{
			GD.PrintErr("CameraController: Camera2D child not found!");
		}
		else
		{
			_camera.Enabled = true;
			GD.Print("CameraController: Camera2D ready and enabled");
		}
	}

	public override void _Process(double delta)
	{
		if (_camera == null || _target == null) return;

		// Update based on camera mode
		switch (_mode)
		{
			case CameraMode.Follow:
				UpdateFollowMode(delta);
				break;
			case CameraMode.Fixed:
				UpdateFixedMode(delta);
				break;
			case CameraMode.Directed:
				UpdateDirectedMode(delta);
				break;
			case CameraMode.Cutscene:
				// Cutscene mode is controlled externally
				break;
		}

		// Apply screen shake
		UpdateScreenShake(delta);

		// Apply rotation if enabled
		if (_rotateWithTarget)
		{
			UpdateRotation(delta);
		}
	}

	private void UpdateFollowMode(double delta)
	{
		// Calculate look-ahead based on target velocity
		Vector2 targetVelocity = Vector2.Zero;
		if (_target is CharacterBody2D character)
		{
			targetVelocity = character.Velocity;
		}

		Vector2 desiredLookAhead = targetVelocity.Normalized() * _lookAheadDistance;
		_lookAheadOffset = _lookAheadOffset.Lerp(desiredLookAhead, _lookAheadSpeed * (float)delta);

		// Calculate target position with offsets
		Vector2 targetPosition = _target.GlobalPosition + _followOffset + _lookAheadOffset + _directorOffset;

		// Apply boundaries if enabled
		if (_useBoundaries)
		{
			targetPosition.X = Mathf.Clamp(targetPosition.X, _cameraBounds.Position.X, _cameraBounds.End.X);
			targetPosition.Y = Mathf.Clamp(targetPosition.Y, _cameraBounds.Position.Y, _cameraBounds.End.Y);
		}

		// Smooth spring follow - apply to camera
		_camera.GlobalPosition = _camera.GlobalPosition.Lerp(targetPosition, _followSpeed * (float)delta);
	}

	private void UpdateDirectedMode(double delta)
	{
		// Similar to follow mode but with additional director offset
		_directorOffset = _directorOffset.Lerp(_targetDirectorOffset, _directorOffsetSpeed * (float)delta);

		// Once we reach the target offset, switch back to follow mode
		if (_directorOffset.DistanceTo(_targetDirectorOffset) < 1.0f)
		{
			_mode = CameraMode.Follow;
		}

		UpdateFollowMode(delta);
	}

	private void UpdateFixedMode(double delta)
	{
		// Smoothly move to fixed position
		_camera.GlobalPosition = _camera.GlobalPosition.Lerp(_fixedPosition, _followSpeed * (float)delta);

		if (_rotateWithTarget)
		{
			_camera.GlobalRotation = Mathf.LerpAngle(_camera.GlobalRotation, _fixedRotation, _rotationSpeed * (float)delta);
		}
	}

	private void UpdateRotation(double delta)
	{
		// Get target's up direction and convert to rotation
		Vector2 targetUp = _target.Transform.Y.Normalized();
		float targetRotation = targetUp.Angle() - Mathf.Pi / 2; // -90 degrees because up is -Y

		// Smooth rotation - apply to camera
		_camera.GlobalRotation = Mathf.LerpAngle(_camera.GlobalRotation, targetRotation, _rotationSpeed * (float)delta);
	}

	private void UpdateScreenShake(double delta)
	{
		if (_shakeStrength > 0)
		{
			_shakeTime += (float)delta;

			// Apply shake offset to camera
			float shakeX = (float)(GD.RandRange(-1.0, 1.0) * _shakeStrength);
			float shakeY = (float)(GD.RandRange(-1.0, 1.0) * _shakeStrength);
			_camera.Offset = new Vector2(shakeX, shakeY);

			// Decay shake over time
			_shakeStrength = Mathf.Max(0, _shakeStrength - _shakeDecayRate * (float)delta);
		}
		else
		{
			_camera.Offset = Vector2.Zero;
		}
	}

	#region Public API

	/// <summary>
	/// Apply screen shake effect
	/// </summary>
	public void Shake(float strength, float frequency = 20.0f)
	{
		_shakeStrength = Mathf.Max(_shakeStrength, strength); // Use strongest shake
		_shakeFrequency = frequency;
		_shakeTime = 0.0f;
	}

	/// <summary>
	/// Temporarily offset camera to show something in the level.
	/// Camera will smoothly return to follow mode afterward.
	/// </summary>
	public void DirectAttention(Vector2 offset, float speed = 2.0f)
	{
		_targetDirectorOffset = offset;
		_directorOffsetSpeed = speed;
		_mode = CameraMode.Directed;
		GD.Print($"Camera directing attention to offset: {offset}");
	}

	/// <summary>
	/// Reset director offset and return to normal follow
	/// </summary>
	public void ResetDirectorOffset(float speed = 2.0f)
	{
		_targetDirectorOffset = Vector2.Zero;
		_directorOffsetSpeed = speed;
	}

	/// <summary>
	/// Lock camera to a specific position (for boss rooms, etc.)
	/// </summary>
	public void LockToPosition(Vector2 position, float rotation = 0.0f)
	{
		_fixedPosition = position;
		_fixedRotation = rotation;
		_mode = CameraMode.Fixed;
		GD.Print($"Camera locked to position: {position}");
	}

	/// <summary>
	/// Return to following the target
	/// </summary>
	public void ReturnToFollow()
	{
		_mode = CameraMode.Follow;
		_directorOffset = Vector2.Zero;
		_targetDirectorOffset = Vector2.Zero;
		GD.Print("Camera returned to follow mode");
	}

	/// <summary>
	/// Enter cutscene mode - camera position is controlled externally
	/// </summary>
	public void EnterCutsceneMode()
	{
		_mode = CameraMode.Cutscene;
		GD.Print("Camera entered cutscene mode");
	}

	/// <summary>
	/// Set camera boundaries (useful for keeping camera within level bounds)
	/// </summary>
	public void SetBoundaries(Rect2 bounds, bool enabled = true)
	{
		_cameraBounds = bounds;
		_useBoundaries = enabled;
	}

	/// <summary>
	/// Get the Camera2D node for advanced effects
	/// </summary>
	public Camera2D GetCamera()
	{
		return _camera;
	}

	/// <summary>
	/// Set new target to follow
	/// </summary>
	public void SetTarget(Node2D newTarget)
	{
		_target = newTarget;
	}

	#endregion
}
