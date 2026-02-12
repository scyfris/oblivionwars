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
    [Export] public Rect2 CameraBounds = new Rect2(-10000, -10000, 20000, 20000);

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
            nameof(UseBoundaries) or nameof(CameraBounds) => UseDefaultBoundaries,
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

    public Rect2 GetCameraBounds(CameraSettings defaults) =>
        UseDefaultBoundaries ? defaults.CameraBounds : CameraBounds;

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
