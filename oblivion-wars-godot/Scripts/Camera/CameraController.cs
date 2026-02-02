using Godot;
using System;
using System.Linq.Expressions;

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
	public static CameraController Instance { get; private set; }

	[ExportGroup("Target")]

	/// <summary>
	/// The player character or object for the camera to follow
	/// </summary>
	[Export] private Node2D _target;

	/// <summary>
	/// The Camera2D node that this controller will manipulate
	/// </summary>
	[Export] private Camera2D _camera;

	[ExportGroup("Follow Settings")]

	/// <summary>
	/// How quickly the camera catches up to the target (higher = tighter following, lower = more lag)
	/// </summary>
	[Export] private float _followSpeed = 5.0f;

	/// <summary>
	/// Minimum speed in pixels/second the camera moves when returning to player (prevents slow falloff)
	/// </summary>
	[Export] private float _minFollowSpeed = 200.0f;

	/// <summary>
	/// Static offset from the target's position (useful for framing the character)
	/// </summary>
	[Export] private Vector2 _followOffset = Vector2.Zero;

	/// <summary>
	/// Deadzone size - player can move this far from camera center before camera starts following (X = horizontal, Y = vertical)
	/// </summary>
	[Export] private Vector2 _deadzone = new Vector2(40.0f, 30.0f);

	/// <summary>
	/// How far ahead of the target to look based on movement direction (creates anticipation)
	/// </summary>
	[Export] private float _lookAheadDistance = 50.0f;

	/// <summary>
	/// How quickly the look-ahead offset adjusts to velocity changes
	/// </summary>
	[Export] private float _lookAheadSpeed = 2.0f;

	[ExportGroup("Boundaries")]

	/// <summary>
	/// Whether to constrain camera movement within defined bounds
	/// </summary>
	[Export] private bool _useBoundaries = false;

	/// <summary>
	/// Rectangle defining the area the camera can move within (only active if UseBoundaries is true)
	/// </summary>
	[Export] private Rect2 _cameraBounds = new Rect2(-10000, -10000, 20000, 20000);

	[ExportGroup("Rotation (Gravity Flip)")]

	/// <summary>
	/// Whether camera should rotate to match player's gravity orientation
	/// </summary>
	[Export] private bool _rotateWithPlayer = true;

	/// <summary>
	/// How quickly the camera rotates to match player orientation (higher = faster rotation)
	/// </summary>
	[Export] private float _rotationSpeed = 5.0f;

	/// <summary>
	/// Minimum rotation speed in radians/second (prevents slow falloff at end of rotation)
	/// </summary>
	[Export] private float _minRotationSpeed = 3.0f;

	/// <summary>
	/// Delay in seconds before camera starts rotating after gravity change (creates dramatic effect)
	/// </summary>
	[Export] private float _rotationDelay = 0.3f;

	[ExportGroup("Screen Shake")]

	/// <summary>
	/// How quickly screen shake effect fades out (higher = shake ends faster)
	/// </summary>
	[Export] private float _shakeDecayRate = 3.0f;

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

	// Camera rotation (for gravity flip)
	private float _targetRotation = 0.0f;
	private float _rotationDelayTimer = 0.0f;
	private bool _isDelayingRotation = false;

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
		if (Instance != null)
		{
			GD.PrintErr("CameraController: Duplicate instance detected, removing this one.");
			QueueFree();
			return;
		}
		Instance = this;

		if (_camera == null)
		{
			GD.PrintErr("CameraController: Camera2D not assigned!");
			return;
		}

		// Ensure camera is centered on its position (not top-left anchored) - set BEFORE making current
		_camera.AnchorMode = Camera2D.AnchorModeEnum.DragCenter;
		_camera.PositionSmoothingEnabled = false; // Disable smoothing for immediate positioning

		_camera.Enabled = true;
		_camera.MakeCurrent();

		// Position camera on target immediately
		if (_target != null)
		{
			_camera.GlobalPosition = _target.GlobalPosition + _followOffset;
			_camera.ResetSmoothing(); // Force camera to snap to position
			GD.Print($"CameraController: Camera GlobalPos={_camera.GlobalPosition}, Target={_target.GlobalPosition}");
		}

		GD.Print("CameraController: Camera2D ready, enabled, and made current");
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public override void _PhysicsProcess(double delta)
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

		// Apply camera rotation (for gravity flip)
		if (_rotateWithPlayer)
		{
			UpdateRotation(delta);
		}

		// Apply screen shake
		UpdateScreenShake(delta);
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

		// Calculate ideal target position with offsets
		Vector2 idealTargetPosition = _target.GlobalPosition + _followOffset + _lookAheadOffset + _directorOffset;

		// Apply deadzone - camera only moves if target is outside the deadzone
		Vector2 cameraToTarget = idealTargetPosition - _camera.GlobalPosition;
		Vector2 deadzoneOffset = Vector2.Zero;

		// Check horizontal deadzone
		if (Mathf.Abs(cameraToTarget.X) > _deadzone.X)
		{
			// Outside deadzone - calculate how far outside
			float sign = Mathf.Sign(cameraToTarget.X);
			deadzoneOffset.X = cameraToTarget.X - (sign * _deadzone.X);
		}

		// Check vertical deadzone
		if (Mathf.Abs(cameraToTarget.Y) > _deadzone.Y)
		{
			// Outside deadzone - calculate how far outside
			float sign = Mathf.Sign(cameraToTarget.Y);
			deadzoneOffset.Y = cameraToTarget.Y - (sign * _deadzone.Y);
		}

		// Calculate target position (camera center + offset needed to keep player in deadzone)
		Vector2 targetPosition = _camera.GlobalPosition + deadzoneOffset;

		// Apply boundaries if enabled
		if (_useBoundaries)
		{
			targetPosition.X = Mathf.Clamp(targetPosition.X, _cameraBounds.Position.X, _cameraBounds.End.X);
			targetPosition.Y = Mathf.Clamp(targetPosition.Y, _cameraBounds.Position.Y, _cameraBounds.End.Y);
		}

		// Calculate distance to target
		float distanceToTarget = _camera.GlobalPosition.DistanceTo(targetPosition);

		if (distanceToTarget > 0.1f) // Only move if not already at target
		{
			// Calculate lerp speed with minimum speed guarantee
			float lerpAmount = _followSpeed * (float)delta;

			// Calculate what the lerp would move us this frame
			float lerpDistance = distanceToTarget * lerpAmount;

			// Ensure we move at least the minimum speed
			float minDistance = _minFollowSpeed * (float)delta;

			if (lerpDistance < minDistance && distanceToTarget > minDistance)
			{
				// Lerp would be too slow, use minimum speed instead
				Vector2 direction = (targetPosition - _camera.GlobalPosition).Normalized();
				_camera.GlobalPosition += direction * minDistance;
			}
			else
			{
				// Normal lerp
				_camera.GlobalPosition = _camera.GlobalPosition.Lerp(targetPosition, lerpAmount);
			}
		}

		// Debug output (only on first few frames)
		if (Engine.GetProcessFrames() < 5)
		{
			GD.Print($"CameraController: Frame {Engine.GetProcessFrames()} - Camera {_camera.GlobalPosition}, Target: {idealTargetPosition}, Offset: {deadzoneOffset}");
		}
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
	}

	private void UpdateRotation(double delta)
	{
		if (_target is not CharacterBody2D character) return;

		// Get target rotation from player's UpDirection (gravity-based, not visual transform)
		Vector2 targetUp = character.UpDirection;

		// Calculate target rotation based on up direction
		float newTargetRotation;
		if (targetUp.X == 1 && targetUp.Y == 0)
		{
			newTargetRotation = Mathf.Pi / 2;
		}
		else if (targetUp.X == -1 && targetUp.Y == 0)
		{
			newTargetRotation = -Mathf.Pi / 2;
		}
		else if (targetUp.Y == 1 && targetUp.X == 0)
		{
			newTargetRotation = Mathf.Pi;
		}
		else if (targetUp.Y == -1 && targetUp.X == 0)
		{
			newTargetRotation = 0;
		}
		else
		{
			// Fallback calculation
			newTargetRotation = targetUp.Angle() + Mathf.Pi / 2;
		}

		// Detect rotation change
		float angleDiff = Mathf.Abs(Mathf.AngleDifference(_targetRotation, newTargetRotation));
		if (angleDiff > 0.1f)
		{
			_targetRotation = newTargetRotation;
			_isDelayingRotation = true;
			_rotationDelayTimer = _rotationDelay;
			GD.Print($"CameraController: Rotation change detected, starting delay ({_rotationDelay}s)");
		}

		// Handle delay countdown
		if (_isDelayingRotation)
		{
			_rotationDelayTimer -= (float)delta;
			if (_rotationDelayTimer <= 0)
			{
				_isDelayingRotation = false;
				GD.Print("CameraController: Delay expired, starting rotation");
			}
			else
			{
				return; // Still delaying
			}
		}

		// Calculate angular distance to target
		float angularDistance = Mathf.Abs(Mathf.AngleDifference(_camera.GlobalRotation, _targetRotation));

		if (angularDistance > 0.01f) // Only rotate if not already at target
		{
			// Calculate lerp amount
			float lerpAmount = _rotationSpeed * (float)delta;

			// Calculate what the lerp would rotate us this frame
			float lerpRotation = angularDistance * lerpAmount;

			// Ensure we rotate at least the minimum speed
			float minRotation = _minRotationSpeed * (float)delta;

			float direction = Mathf.Sign(Mathf.AngleDifference(_camera.GlobalRotation, _targetRotation));

			// Determine how much to rotate this frame
			if (angularDistance <= minRotation)
			{
				// Very close - just apply remaining distance to finish rotation
				_camera.GlobalRotation += direction * angularDistance;
			}
			else if (lerpRotation < minRotation)
			{
				// Lerp would be too slow, use minimum speed instead
				_camera.GlobalRotation += direction * minRotation;
			}
			else
			{
				// Normal lerp is fast enough
				_camera.GlobalRotation = Mathf.LerpAngle(_camera.GlobalRotation, _targetRotation, lerpAmount);
			}
		}
		else
		{
			// Snap to target when very close
			_camera.GlobalRotation = MathUtils.SnapToNearest90Radians(_targetRotation);
		}
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

	/// <summary>
	/// Set camera rotation immediately without delay (useful for initialization)
	/// </summary>
	public void SetRotationImmediate(float rotation)
	{
		_camera.GlobalRotation = rotation;
		_targetRotation = rotation;
		_isDelayingRotation = false;
	}

	#endregion
}
