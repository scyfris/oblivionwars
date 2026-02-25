using Godot;

[GlobalClass]
public partial class CameraSettings : Resource
{
    // ── Follow ────────────────────────────────────────────
    [ExportGroup("Follow")]

    [Export] public bool UseDefaultFollowSpeed = true;
    [Export] public float FollowSpeed = 5.0f;

    [Export] public bool UseDefaultMinFollowSpeed = true;
    [Export] public float MinFollowSpeed = 200.0f;

    [Export] public bool UseDefaultFollowOffset = true;
    [Export] public Vector2 FollowOffset = Vector2.Zero;

    [Export] public bool UseDefaultDeadzone = true;
    [Export] public Vector2 Deadzone = new Vector2(40.0f, 30.0f);

    [Export] public bool UseDefaultLookAheadDistance = true;
    [Export] public float LookAheadDistance = 50.0f;

    [Export] public bool UseDefaultLookAheadSpeed = true;
    [Export] public float LookAheadSpeed = 2.0f;

    // ── Zoom ──────────────────────────────────────────────
    [ExportGroup("Zoom")]

    [Export] public bool UseDefaultZoom = true;
    [Export] public Vector2 Zoom = new Vector2(1, 1);

    // ── Boundaries ────────────────────────────────────────
    [ExportGroup("Boundaries")]

    [Export] public bool UseDefaultBoundaries = true;
    [Export] public bool UseBoundaries = false;
    [Export] public float BoundLeft = -10000f;
    [Export] public float BoundRight = 10000f;
    /// <summary>Lowest point of bounds (Y-up convention, positive = up).</summary>
    [Export] public float BoundBottom = -10000f;
    /// <summary>Highest point of bounds (Y-up convention, positive = up).</summary>
    [Export] public float BoundTop = 10000f;

    // ── Rotation ──────────────────────────────────────────
    [ExportGroup("Rotation")]

    [Export] public bool UseDefaultRotation = true;
    [Export] public bool RotateWithPlayer = true;
    [Export] public float RotationSpeed = 5.0f;
    [Export] public float MinRotationSpeed = 3.0f;
    [Export] public float RotationDelay = 0.3f;

    // ── Screen Shake ──────────────────────────────────────
    [ExportGroup("Screen Shake")]

    [Export] public bool UseDefaultShake = true;
    [Export] public float BaseShakeStrength = 5.0f;
    [Export] public float BaseShakeDuration = 0.3f;

    // ── Inspector: grey out fields when UseDefault is checked ──

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        string name = property["name"].AsString();

        bool readOnly = name switch
        {
            nameof(FollowSpeed) => UseDefaultFollowSpeed,
            nameof(MinFollowSpeed) => UseDefaultMinFollowSpeed,
            nameof(FollowOffset) => UseDefaultFollowOffset,
            nameof(Deadzone) => UseDefaultDeadzone,
            nameof(LookAheadDistance) => UseDefaultLookAheadDistance,
            nameof(LookAheadSpeed) => UseDefaultLookAheadSpeed,
            nameof(Zoom) => UseDefaultZoom,
            nameof(UseBoundaries) or nameof(BoundLeft) or nameof(BoundRight)
                or nameof(BoundBottom) or nameof(BoundTop) => UseDefaultBoundaries,
            nameof(RotateWithPlayer) or nameof(RotationSpeed)
                or nameof(MinRotationSpeed) or nameof(RotationDelay) => UseDefaultRotation,
            nameof(BaseShakeStrength) or nameof(BaseShakeDuration) => UseDefaultShake,
            _ => false,
        };

        if (readOnly)
        {
            var usage = (PropertyUsageFlags)property["usage"].AsInt64();
            property["usage"] = (int)(usage | PropertyUsageFlags.ReadOnly);
        }
    }

    // ── Resolved getters (pass the controller's default settings) ──

    public float GetFollowSpeed(CameraSettings defaults) =>
        UseDefaultFollowSpeed ? defaults.FollowSpeed : FollowSpeed;

    public float GetMinFollowSpeed(CameraSettings defaults) =>
        UseDefaultMinFollowSpeed ? defaults.MinFollowSpeed : MinFollowSpeed;

    public Vector2 GetFollowOffset(CameraSettings defaults) =>
        UseDefaultFollowOffset ? defaults.FollowOffset : FollowOffset;

    public Vector2 GetDeadzone(CameraSettings defaults) =>
        UseDefaultDeadzone ? defaults.Deadzone : Deadzone;

    public float GetLookAheadDistance(CameraSettings defaults) =>
        UseDefaultLookAheadDistance ? defaults.LookAheadDistance : LookAheadDistance;

    public float GetLookAheadSpeed(CameraSettings defaults) =>
        UseDefaultLookAheadSpeed ? defaults.LookAheadSpeed : LookAheadSpeed;

    public Vector2 GetZoom(CameraSettings defaults) =>
        UseDefaultZoom ? defaults.Zoom : Zoom;

    public bool GetUseBoundaries(CameraSettings defaults) =>
        UseDefaultBoundaries ? defaults.UseBoundaries : UseBoundaries;

    public Rect2 GetCameraBounds(CameraSettings defaults)
    {
        var s = UseDefaultBoundaries ? defaults : this;
        // Convert Y-up convention to Godot's Y-down: negate and swap top/bottom
        float godotMinY = -s.BoundTop;
        float godotMaxY = -s.BoundBottom;
        return new Rect2(s.BoundLeft, godotMinY, s.BoundRight - s.BoundLeft, godotMaxY - godotMinY);
    }

    public bool GetRotateWithPlayer(CameraSettings defaults) =>
        UseDefaultRotation ? defaults.RotateWithPlayer : RotateWithPlayer;

    public float GetRotationSpeed(CameraSettings defaults) =>
        UseDefaultRotation ? defaults.RotationSpeed : RotationSpeed;

    public float GetMinRotationSpeed(CameraSettings defaults) =>
        UseDefaultRotation ? defaults.MinRotationSpeed : MinRotationSpeed;

    public float GetRotationDelay(CameraSettings defaults) =>
        UseDefaultRotation ? defaults.RotationDelay : RotationDelay;

    public float GetBaseShakeStrength(CameraSettings defaults) =>
        UseDefaultShake ? defaults.BaseShakeStrength : BaseShakeStrength;

    public float GetBaseShakeDuration(CameraSettings defaults) =>
        UseDefaultShake ? defaults.BaseShakeDuration : BaseShakeDuration;
}
