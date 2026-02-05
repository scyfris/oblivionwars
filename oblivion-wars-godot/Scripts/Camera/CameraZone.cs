using Godot;
using System;

[Flags]
public enum GravityFilter
{
	Down  = 1,   // 0°   — normal gravity
	Right = 2,   // 90°  — gravity pulls right
	Up    = 4,   // 180° — inverted gravity
	Left  = 8,   // 270° — gravity pulls left
}

/// <summary>
/// Area-based camera constraint zone. Place as an Area2D with a RectangleShape2D
/// collision shape to define the zone bounds. When the player enters, the camera
/// is constrained to the zone bounds and camera parameters can be overridden.
///
/// Constraints are always in world space (W suffix). Gravity-filtered zones let
/// you have overlapping zones where the active zone depends on the player's
/// current gravity orientation.
/// </summary>
public partial class CameraZone : Area2D
{
	// ── Gravity Filter ──────────────────────────────────────
	[ExportGroup("Gravity Filter")]

	/// <summary>
	/// If true, this zone applies regardless of gravity. If false, only applies
	/// when the player's gravity matches GravityRotations.
	/// </summary>
	[Export] public bool IsDefaultZone = true;

	/// <summary>
	/// Which gravity orientations activate this zone (only used when IsDefaultZone is false).
	/// </summary>
	[Export(PropertyHint.Flags, "Down,Right,Up,Left")]
	public GravityFilter GravityRotations = 0;

	// ── Constraint Edges (World Space) ──────────────────────
	//    Each edge: enable flag + offset from collision shape edge
	//    Positive offset = shift inward (tighter), negative = outward (looser)
	[ExportGroup("Constraint Edges (World Space)")]

	[Export] public bool ConstrainMinXW = true;
	[Export] public float MinXOffsetW = 0f;

	[Export] public bool ConstrainMaxXW = true;
	[Export] public float MaxXOffsetW = 0f;

	[Export] public bool ConstrainMinYW = true;
	[Export] public float MinYOffsetW = 0f;

	[Export] public bool ConstrainMaxYW = true;
	[Export] public float MaxYOffsetW = 0f;

	// ── Axis Locking ────────────────────────────────────────
	[ExportGroup("Axis Locking")]

	/// <summary>Lock camera X to zone center (world space)</summary>
	[Export] public bool LockAxisXW = false;

	/// <summary>Lock camera Y to zone center (world space)</summary>
	[Export] public bool LockAxisYW = false;

	// ── Camera Parameter Overrides ──────────────────────────
	//    Each override: bool flag + value. If flag is false, CameraController uses its default.
	[ExportGroup("Camera Overrides")]

	[Export] public bool OverrideFollowSpeed = false;
	[Export] public float FollowSpeedL = 5.0f;

	[Export] public bool OverrideFollowOffset = false;
	[Export] public Vector2 FollowOffsetL = Vector2.Zero;

	[Export] public bool OverrideDeadzone = false;
	[Export] public Vector2 DeadzoneL = new Vector2(40, 30);

	[Export] public bool OverrideLookAheadDistance = false;
	[Export] public float LookAheadDistanceL = 50.0f;

	[Export] public bool OverrideZoom = false;
	[Export] public Vector2 ZoomL = new Vector2(1, 1);

	// ── Internal ────────────────────────────────────────────
	private Rect2 _worldBounds;

	public override void _Ready()
	{
		// Compute world bounds from first CollisionShape2D child with a RectangleShape2D
		foreach (var child in GetChildren())
		{
			if (child is CollisionShape2D cs && cs.Shape is RectangleShape2D rect)
			{
				Vector2 halfSize = rect.Size / 2;
				Vector2 center = cs.GlobalPosition;
				_worldBounds = new Rect2(center - halfSize, rect.Size);
				break;
			}
		}

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not CharacterBody2D) return;
		CameraController.Instance?.EnterZone(this);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D) return;
		CameraController.Instance?.ExitZone(this);
	}

	/// <summary>
	/// Check if this zone matches the given gravity filter.
	/// Default zones always match.
	/// </summary>
	public bool MatchesGravity(GravityFilter current)
	{
		if (IsDefaultZone) return true;
		return (GravityRotations & current) != 0;
	}

	/// <summary>
	/// Get effective world-space bounds with offsets and lock flags applied.
	/// When an axis is locked, both min and max return the zone center on that axis.
	/// </summary>
	public (float minX, float maxX, float minY, float maxY) GetEffectiveBounds()
	{
		float centerX = _worldBounds.GetCenter().X;
		float centerY = _worldBounds.GetCenter().Y;

		return (
			minX: LockAxisXW ? centerX : _worldBounds.Position.X + MinXOffsetW,
			maxX: LockAxisXW ? centerX : _worldBounds.End.X - MaxXOffsetW,
			minY: LockAxisYW ? centerY : _worldBounds.Position.Y + MinYOffsetW,
			maxY: LockAxisYW ? centerY : _worldBounds.End.Y - MaxYOffsetW
		);
	}
}
