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
[Tool]
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

	[Export] public bool ConstrainMinXWorld = true;
	[Export] public float MinXOffsetWorld = 0f;

	[Export] public bool ConstrainMaxXWorld = true;
	[Export] public float MaxXOffsetWorld = 0f;

	[Export] public bool ConstrainMinYWorld = true;
	[Export] public float MinYOffsetWorld = 0f;

	[Export] public bool ConstrainMaxYWorld = true;
	[Export] public float MaxYOffsetWorld = 0f;

	// ── Axis Locking ────────────────────────────────────────
	[ExportGroup("Axis Locking")]

	/// <summary>Lock camera X to zone center (world space)</summary>
	[Export] public bool LockAxisXWorld = false;

	/// <summary>Lock camera Y to zone center (world space)</summary>
	[Export] public bool LockAxisYWorld = false;

	// ── Player-Relative Distance Constraints (Local Space) ──
	//    Clamp how far the camera can stray from the player on each axis
	//    in the player's local coordinate system (rotates with gravity).
	//    "Above" = opposite to gravity, "Below" = toward gravity,
	//    "Left"/"Right" = perpendicular to gravity.
	[ExportGroup("Player-Relative Constraints (Local)")]

	[Export] public bool ConstrainDistanceAbove = false;
	[Export] public float MaxDistanceAbovePixels = 200f;

	[Export] public bool ConstrainDistanceBelow = false;
	[Export] public float MaxDistanceBelowPixels = 200f;

	[Export] public bool ConstrainDistanceLeft = false;
	[Export] public float MaxDistanceLeftPixels = 200f;

	[Export] public bool ConstrainDistanceRight = false;
	[Export] public float MaxDistanceRightPixels = 200f;

	// ── Camera Parameter Overrides ──────────────────────────
	//    Each override: bool flag + value. If flag is false, CameraController uses its default.
	[ExportGroup("Camera Overrides")]

	[Export] public bool OverrideFollowSpeed = false;
	[Export] public float FollowSpeed = 5.0f;

	[Export] public bool OverrideFollowOffset = false;
	[Export] public Vector2 FollowOffset = Vector2.Zero;

	[Export] public bool OverrideDeadzone = false;
	[Export] public Vector2 DeadzonePixels = new Vector2(40, 30);

	[Export] public bool OverrideLookAheadDistance = false;
	[Export] public float LookAheadDistancePixels = 50.0f;

	[Export] public bool OverrideZoom = false;
	[Export] public Vector2 Zoom = new Vector2(1, 1);

	// ── Internal ────────────────────────────────────────────
	private Rect2 _worldBounds;

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return;

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

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			QueueRedraw();
	}

	public override void _Draw()
	{
		if (!Engine.IsEditorHint()) return;

		// Find the CollisionShape2D child to get local-space bounds
		CollisionShape2D collisionShape = null;
		RectangleShape2D rectShape = null;
		foreach (var child in GetChildren())
		{
			if (child is CollisionShape2D c && c.Shape is RectangleShape2D r)
			{
				collisionShape = c;
				rectShape = r;
				break;
			}
		}
		if (collisionShape == null || rectShape == null) return;

		Vector2 halfSize = rectShape.Size / 2;
		Vector2 center = collisionShape.Position; // local to this node
		float left = center.X - halfSize.X;
		float right = center.X + halfSize.X;
		float top = center.Y - halfSize.Y;
		float bottom = center.Y + halfSize.Y;

		// Colors
		Color enabledColor = new Color(0.2f, 1f, 0.2f, 0.9f);      // bright green
		Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);   // dim gray
		Color lockColor = new Color(1f, 0.5f, 0f, 0.9f);            // orange
		Color boundsColor = new Color(0.3f, 0.7f, 1f, 0.3f);        // light blue

		float thinLine = 1.5f;
		float thickLine = 3f;

		// Draw full collision bounds as a dim rectangle outline
		DrawRect(new Rect2(center - halfSize, rectShape.Size), boundsColor, false, thinLine);

		// ── X-axis constraints ──
		if (LockAxisXWorld)
		{
			// Vertical center line (orange)
			DrawLine(new Vector2(center.X, top), new Vector2(center.X, bottom), lockColor, thickLine);
		}
		else
		{
			// MinX (left edge with offset)
			float minX = left + MinXOffsetWorld;
			Color minXColor = ConstrainMinXWorld ? enabledColor : disabledColor;
			float minXWidth = ConstrainMinXWorld ? thickLine : thinLine;
			DrawLine(new Vector2(minX, top), new Vector2(minX, bottom), minXColor, minXWidth);

			// MaxX (right edge with offset)
			float maxX = right - MaxXOffsetWorld;
			Color maxXColor = ConstrainMaxXWorld ? enabledColor : disabledColor;
			float maxXWidth = ConstrainMaxXWorld ? thickLine : thinLine;
			DrawLine(new Vector2(maxX, top), new Vector2(maxX, bottom), maxXColor, maxXWidth);
		}

		// ── Y-axis constraints ──
		if (LockAxisYWorld)
		{
			// Horizontal center line (orange)
			DrawLine(new Vector2(left, center.Y), new Vector2(right, center.Y), lockColor, thickLine);
		}
		else
		{
			// MinY (top edge with offset)
			float minY = top + MinYOffsetWorld;
			Color minYColor = ConstrainMinYWorld ? enabledColor : disabledColor;
			float minYWidth = ConstrainMinYWorld ? thickLine : thinLine;
			DrawLine(new Vector2(left, minY), new Vector2(right, minY), minYColor, minYWidth);

			// MaxY (bottom edge with offset)
			float maxY = bottom - MaxYOffsetWorld;
			Color maxYColor = ConstrainMaxYWorld ? enabledColor : disabledColor;
			float maxYWidth = ConstrainMaxYWorld ? thickLine : thinLine;
			DrawLine(new Vector2(left, maxY), new Vector2(right, maxY), maxYColor, maxYWidth);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not CharacterBody2D) return;
		GD.Print($"CameraZone '{Name}': Player entered. Bounds={_worldBounds}");
		CameraController.Instance?.EnterZone(this);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D) return;
		GD.Print($"CameraZone '{Name}': Player exited.");
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
	/// Whether any player-relative distance constraints are enabled on this zone.
	/// </summary>
	public bool HasPlayerRelativeConstraints =>
		ConstrainDistanceAbove || ConstrainDistanceBelow ||
		ConstrainDistanceLeft || ConstrainDistanceRight;

	/// <summary>
	/// Get effective world-space bounds with offsets and lock flags applied.
	/// When an axis is locked, both min and max return the zone center on that axis.
	/// </summary>
	public (float minX, float maxX, float minY, float maxY) GetEffectiveBounds()
	{
		float centerX = _worldBounds.GetCenter().X;
		float centerY = _worldBounds.GetCenter().Y;

		return (
			minX: LockAxisXWorld ? centerX : _worldBounds.Position.X + MinXOffsetWorld,
			maxX: LockAxisXWorld ? centerX : _worldBounds.End.X - MaxXOffsetWorld,
			minY: LockAxisYWorld ? centerY : _worldBounds.Position.Y + MinYOffsetWorld,
			maxY: LockAxisYWorld ? centerY : _worldBounds.End.Y - MaxYOffsetWorld
		);
	}
}
