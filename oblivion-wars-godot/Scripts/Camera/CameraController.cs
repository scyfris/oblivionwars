using Godot;
using System.Collections.Generic;

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
	/// Base screen shake intensity in pixels
	/// </summary>
	[Export] private float _baseShakeStrength = 5.0f;

	/// <summary>
	/// Base duration in seconds for the shake to decay to zero
	/// </summary>
	[Export] private float _baseShakeDuration = 0.3f;

	private Vector2 _velocity = Vector2.Zero;
	private Vector2 _lookAheadOffset = Vector2.Zero;
	private Vector2 _currentOffset = Vector2.Zero;

	// Screen shake
	private float _shakeStrength = 0.0f;
	private float _shakeDecayRate = 0.0f;

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

	// Camera zones
	private List<CameraZone> _occupiedZones = new();
	private CameraZone _activeZone = null;
	private Vector2 _zoneFollowOffset = Vector2.Zero;
	private Vector2 _currentZoom = new Vector2(1, 1);
	private Vector2 _targetZoom = new Vector2(1, 1);

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

		// Initialize zone follow offset to default so there's no lerp on startup
		_zoneFollowOffset = _followOffset;

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
		// Re-evaluate active zone each frame (gravity may have changed)
		ResolveActiveZone();

		// Compute effective parameters (zone overrides or defaults)
		float effectiveFollowSpeed = (_activeZone?.OverrideFollowSpeed == true)
			? _activeZone.FollowSpeed : _followSpeed;
		Vector2 effectiveDeadzone = (_activeZone?.OverrideDeadzone == true)
			? _activeZone.DeadzonePixels : _deadzone;
		float effectiveLookAhead = (_activeZone?.OverrideLookAheadDistance == true)
			? _activeZone.LookAheadDistancePixels : _lookAheadDistance;

		// Lerp follow offset toward zone value (smooth transition, no position jump)
		Vector2 targetFollowOffset = (_activeZone?.OverrideFollowOffset == true)
			? _activeZone.FollowOffset : _followOffset;
		_zoneFollowOffset = _zoneFollowOffset.Lerp(targetFollowOffset, effectiveFollowSpeed * (float)delta);

		// Lerp zoom toward zone value
		_targetZoom = (_activeZone?.OverrideZoom == true)
			? _activeZone.Zoom : new Vector2(1, 1);
		_currentZoom = _currentZoom.Lerp(_targetZoom, effectiveFollowSpeed * (float)delta);
		_camera.Zoom = _currentZoom;

		// Calculate look-ahead based on target velocity
		Vector2 targetVelocity = Vector2.Zero;
		if (_target is CharacterBody2D character)
		{
			targetVelocity = character.Velocity;
		}

		Vector2 desiredLookAhead = targetVelocity.Normalized() * effectiveLookAhead;
		_lookAheadOffset = _lookAheadOffset.Lerp(desiredLookAhead, _lookAheadSpeed * (float)delta);

		// Calculate ideal target position with offsets
		Vector2 idealTargetPosition = _target.GlobalPosition + _zoneFollowOffset + _lookAheadOffset + _directorOffset;

		// Apply deadzone - camera only moves if target is outside the deadzone
		Vector2 cameraToTarget = idealTargetPosition - _camera.GlobalPosition;
		Vector2 deadzoneOffset = Vector2.Zero;

		// Check horizontal deadzone
		if (Mathf.Abs(cameraToTarget.X) > effectiveDeadzone.X)
		{
			float sign = Mathf.Sign(cameraToTarget.X);
			deadzoneOffset.X = cameraToTarget.X - (sign * effectiveDeadzone.X);
		}

		// Check vertical deadzone
		if (Mathf.Abs(cameraToTarget.Y) > effectiveDeadzone.Y)
		{
			float sign = Mathf.Sign(cameraToTarget.Y);
			deadzoneOffset.Y = cameraToTarget.Y - (sign * effectiveDeadzone.Y);
		}

		// Calculate target position (camera center + offset needed to keep player in deadzone)
		Vector2 targetPosition = _camera.GlobalPosition + deadzoneOffset;

		// Apply boundaries if enabled
		if (_useBoundaries)
		{
			targetPosition.X = Mathf.Clamp(targetPosition.X, _cameraBounds.Position.X, _cameraBounds.End.X);
			targetPosition.Y = Mathf.Clamp(targetPosition.Y, _cameraBounds.Position.Y, _cameraBounds.End.Y);
		}

		// Apply zone constraints (world-space clamping)
		if (_activeZone != null)
		{
			var (minX, maxX, minY, maxY) = _activeZone.GetEffectiveBounds();

			if (_activeZone.ConstrainMinXWorld || _activeZone.LockAxisXWorld)
				targetPosition.X = Mathf.Max(targetPosition.X, minX);
			if (_activeZone.ConstrainMaxXWorld || _activeZone.LockAxisXWorld)
				targetPosition.X = Mathf.Min(targetPosition.X, maxX);
			if (_activeZone.ConstrainMinYWorld || _activeZone.LockAxisYWorld)
				targetPosition.Y = Mathf.Max(targetPosition.Y, minY);
			if (_activeZone.ConstrainMaxYWorld || _activeZone.LockAxisYWorld)
				targetPosition.Y = Mathf.Min(targetPosition.Y, maxY);

			// Apply player-relative distance constraints (local space)
			if (_activeZone.HasPlayerRelativeConstraints && _target is CharacterBody2D charBody)
			{
				Vector2 playerPos = charBody.GlobalPosition;
				Vector2 localUp = charBody.UpDirection;
				Vector2 localRight = new Vector2(localUp.Y, -localUp.X);

				// Project camera offset from player onto local axes
				Vector2 offset = targetPosition - playerPos;
				float projUp = offset.Dot(localUp);       // positive = above player
				float projRight = offset.Dot(localRight);  // positive = right of player

				bool changed = false;

				// Above = positive projection on localUp
				if (_activeZone.ConstrainDistanceAbove && projUp > _activeZone.MaxDistanceAbovePixels)
				{
					projUp = _activeZone.MaxDistanceAbovePixels;
					changed = true;
				}
				// Below = negative projection on localUp
				if (_activeZone.ConstrainDistanceBelow && projUp < -_activeZone.MaxDistanceBelowPixels)
				{
					projUp = -_activeZone.MaxDistanceBelowPixels;
					changed = true;
				}
				// Right = positive projection on localRight
				if (_activeZone.ConstrainDistanceRight && projRight > _activeZone.MaxDistanceRightPixels)
				{
					projRight = _activeZone.MaxDistanceRightPixels;
					changed = true;
				}
				// Left = negative projection on localRight
				if (_activeZone.ConstrainDistanceLeft && projRight < -_activeZone.MaxDistanceLeftPixels)
				{
					projRight = -_activeZone.MaxDistanceLeftPixels;
					changed = true;
				}

				if (changed)
				{
					targetPosition = playerPos + localUp * projUp + localRight * projRight;
				}
			}
		}

		// Calculate distance to target
		float distanceToTarget = _camera.GlobalPosition.DistanceTo(targetPosition);

		if (distanceToTarget > 0.1f) // Only move if not already at target
		{
			// Calculate lerp speed with minimum speed guarantee
			float lerpAmount = effectiveFollowSpeed * (float)delta;

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

	#region Camera Zones

	private void ResolveActiveZone()
	{
		GravityFilter currentGravity = GetCurrentGravityFilter();

		CameraZone bestSpecific = null;
		CameraZone bestDefault = null;

		foreach (var zone in _occupiedZones)
		{
			if (!zone.IsDefaultZone && zone.MatchesGravity(currentGravity))
				bestSpecific = zone;
			else if (zone.IsDefaultZone)
				bestDefault = zone;
		}

		_activeZone = bestSpecific ?? bestDefault;
	}

	private GravityFilter GetCurrentGravityFilter()
	{
		if (_target is not CharacterBody2D character) return GravityFilter.Down;
		Vector2 up = character.UpDirection;

		if (up.Y < -0.5f) return GravityFilter.Down;
		if (up.Y > 0.5f)  return GravityFilter.Up;
		if (up.X < -0.5f) return GravityFilter.Right;
		if (up.X > 0.5f)  return GravityFilter.Left;

		return GravityFilter.Down;
	}

	#endregion

	#region Public API

	/// <summary>
	/// Apply screen shake effect. Strength and duration are scaled from the base values.
	/// </summary>
	public void Shake(float strengthScale = 1.0f, float durationScale = 1.0f)
	{
		float strength = _baseShakeStrength * strengthScale;
		float duration = _baseShakeDuration * durationScale;

		_shakeStrength = Mathf.Max(_shakeStrength, strength);
		_shakeDecayRate = duration > 0 ? strength / duration : strength / 0.01f;
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

	/// <summary>
	/// Register a camera zone the player has entered
	/// </summary>
	public void EnterZone(CameraZone zone)
	{
		if (!_occupiedZones.Contains(zone))
			_occupiedZones.Add(zone);
		ResolveActiveZone();
		GD.Print($"CameraController: Entered zone '{zone.Name}', active zone: {_activeZone?.Name ?? "none"}, total zones: {_occupiedZones.Count}");
	}

	/// <summary>
	/// Unregister a camera zone the player has left
	/// </summary>
	public void ExitZone(CameraZone zone)
	{
		_occupiedZones.Remove(zone);
		ResolveActiveZone();
		GD.Print($"CameraController: Exited zone '{zone.Name}', active zone: {_activeZone?.Name ?? "none"}, total zones: {_occupiedZones.Count}");
	}

	#endregion
}
