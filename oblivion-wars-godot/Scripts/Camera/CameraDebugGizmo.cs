using Godot;

/// <summary>
/// [Tool] debug overlay that draws camera boundaries, viewport extents, deadzone, and follow offset
/// in the editor. Export a NodePath to the CameraController node; settings are read via Get() since
/// CameraController is not a [Tool] script.
/// </summary>
[Tool]
public partial class CameraDebugGizmo : Node2D
{
	private const string DefaultSettingsProperty = "_defaultSettings";
	private const string FollowOffsetProperty = "FollowOffset";
	private const string DeadzoneProperty = "Deadzone";
	private const string ZoomProperty = "Zoom";
	private const string UseBoundariesProperty = "UseBoundaries";
	private const string BoundLeftProperty = "BoundLeft";
	private const string BoundRightProperty = "BoundRight";
	private const string BoundBottomProperty = "BoundBottom";
	private const string BoundTopProperty = "BoundTop";

	[Export] private NodePath _cameraControllerPath;

	[ExportGroup("Toggle Layers")]
	[Export] private bool _drawViewport = true;
	[Export] private bool _drawBounds = true;
	[Export] private bool _drawDeadzone = true;
	[Export] private bool _drawFollowOffset = true;

	[ExportGroup("Colors")]
	[Export] private Color _viewportColor = new(0f, 1f, 0f, 0.6f);
	[Export] private Color _boundsColor = new(1f, 0.5f, 0f, 0.6f);
	[Export] private Color _deadzoneColor = new(0f, 0.8f, 1f, 0.5f);
	[Export] private Color _followOffsetColor = new(1f, 1f, 0f, 0.8f);

	[ExportGroup("Line Width")]
	[Export] private float _lineWidth = 2f;

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			QueueRedraw();
	}

	public override void _Draw()
	{
		if (!Engine.IsEditorHint()) return;

		if (_cameraControllerPath == null || _cameraControllerPath.IsEmpty)
			return;

		var controller = GetNodeOrNull(_cameraControllerPath);
		if (controller == null) return;

		// Read the _defaultSettings resource from the CameraController
		var settingsVariant = controller.Get(DefaultSettingsProperty);
		if (settingsVariant.VariantType == Variant.Type.Nil) return;
		var settings = settingsVariant.AsGodotObject();
		if (settings == null) return;

		// Read properties from the settings resource
		Vector2 zoom = GetVector2(settings, ZoomProperty, Vector2.One);
		Vector2 deadzone = GetVector2(settings, DeadzoneProperty, new Vector2(40, 30));
		Vector2 followOffset = GetVector2(settings, FollowOffsetProperty, Vector2.Zero);
		bool useBounds = GetBool(settings, UseBoundariesProperty, false);
		float boundLeft = GetFloat(settings, BoundLeftProperty, -10000f);
		float boundRight = GetFloat(settings, BoundRightProperty, 10000f);
		float boundBottom = GetFloat(settings, BoundBottomProperty, -10000f);
		float boundTop = GetFloat(settings, BoundTopProperty, 10000f);

		// Read viewport size from project settings
		int viewW = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
		int viewH = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
		Vector2 viewportSize = new Vector2(viewW, viewH) / zoom;
		Vector2 halfView = viewportSize / 2f;

		// Draw everything relative to local origin (this node's position = "camera center")
		Vector2 center = Vector2.Zero;

		if (_drawViewport)
		{
			var rect = new Rect2(center - halfView, halfView * 2f);
			DrawRect(rect, _viewportColor, false, _lineWidth);
		}

		if (_drawBounds && useBounds)
		{
			// Convert Y-up convention to Godot Y-down
			float godotMinY = -boundTop;
			float godotMaxY = -boundBottom;
			Vector2 boundsMin = new Vector2(boundLeft, godotMinY) - GlobalPosition;
			Vector2 boundsMax = new Vector2(boundRight, godotMaxY) - GlobalPosition;
			var boundsRect = new Rect2(boundsMin, boundsMax - boundsMin);
			GD.Print($"CameraDebugGizmo: left={boundLeft} right={boundRight} bottom={boundBottom} top={boundTop} godotY=[{godotMinY},{godotMaxY}] rect={boundsRect}");
			DrawRect(boundsRect, _boundsColor, false, _lineWidth);
		}

		if (_drawDeadzone)
		{
			var rect = new Rect2(center - deadzone, deadzone * 2f);
			DrawRect(rect, _deadzoneColor, false, _lineWidth);
		}

		if (_drawFollowOffset)
		{
			Vector2 pos = center + followOffset;
			float size = 8f;
			DrawLine(pos - new Vector2(size, 0), pos + new Vector2(size, 0), _followOffsetColor, _lineWidth);
			DrawLine(pos - new Vector2(0, size), pos + new Vector2(0, size), _followOffsetColor, _lineWidth);
		}
	}

	private static float GetFloat(GodotObject obj, string prop, float fallback)
	{
		var v = obj.Get(prop);
		return v.VariantType != Variant.Type.Nil ? v.AsSingle() : fallback;
	}

	private static bool GetBool(GodotObject obj, string prop, bool fallback)
	{
		var v = obj.Get(prop);
		return v.VariantType != Variant.Type.Nil ? v.AsBool() : fallback;
	}

	private static Vector2 GetVector2(GodotObject obj, string prop, Vector2 fallback)
	{
		var v = obj.Get(prop);
		return v.VariantType != Variant.Type.Nil ? v.AsVector2() : fallback;
	}
}
