using Godot;

/// <summary>
/// [Tool] debug overlay that draws camera boundaries, viewport extents, deadzone, and follow offset.
/// In the editor: reads settings via Get() from an exported NodePath to the CameraController.
/// At runtime: reads directly from CameraController.Instance when debug mode is enabled.
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

	private bool _wasDebugActive;

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			QueueRedraw();
			return;
		}

		var global = GlobalStateManager.Instance?.Global;
		bool debugActive = global?.IsDebugModeEnabled == true && global?.ShowCameraGizmo == true;

		if (debugActive)
		{
			var cam = CameraController.Instance?.GetCamera();
			if (cam != null)
				GlobalPosition = cam.GlobalPosition;
			QueueRedraw();
		}
		else if (_wasDebugActive)
		{
			// Debug just turned off — one final redraw to clear the gizmos
			QueueRedraw();
		}

		_wasDebugActive = debugActive;
	}

	public override void _Draw()
	{
		if (Engine.IsEditorHint())
		{
			DrawEditor();
		}
		else
		{
			var global = GlobalStateManager.Instance?.Global;
			if (global?.IsDebugModeEnabled == true && global?.ShowCameraGizmo == true)
				DrawRuntime();
		}
	}

	private void DrawEditor()
	{
		if (_cameraControllerPath == null || _cameraControllerPath.IsEmpty)
			return;

		var controller = GetNodeOrNull(_cameraControllerPath);
		if (controller == null) return;

		var settingsVariant = controller.Get(DefaultSettingsProperty);
		if (settingsVariant.VariantType == Variant.Type.Nil) return;
		var settings = settingsVariant.AsGodotObject();
		if (settings == null) return;

		Vector2 zoom = GetVector2(settings, ZoomProperty, Vector2.One);
		Vector2 deadzone = GetVector2(settings, DeadzoneProperty, new Vector2(40, 30));
		Vector2 followOffset = GetVector2(settings, FollowOffsetProperty, Vector2.Zero);
		bool useBounds = GetBool(settings, UseBoundariesProperty, false);
		float boundLeft = GetFloat(settings, BoundLeftProperty, -10000f);
		float boundRight = GetFloat(settings, BoundRightProperty, 10000f);
		float boundBottom = GetFloat(settings, BoundBottomProperty, -10000f);
		float boundTop = GetFloat(settings, BoundTopProperty, 10000f);

		int viewW = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
		int viewH = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
		Vector2 viewportSize = new Vector2(viewW, viewH) / zoom;
		Vector2 halfView = viewportSize / 2f;

		Vector2 center = Vector2.Zero;

		DrawGizmos(center, halfView, deadzone, followOffset, useBounds, boundLeft, boundRight, boundBottom, boundTop);
	}

	private void DrawRuntime()
	{
		var controller = CameraController.Instance;
		if (controller == null) return;

		var camera = controller.GetCamera();
		if (camera == null) return;

		var settings = controller.EffectiveSettings;
		var defaults = controller.DefaultSettings;
		if (settings == null || defaults == null) return;

		Vector2 zoom = camera.Zoom;
		Vector2 deadzone = settings.GetDeadzone(defaults);
		Vector2 followOffset = settings.GetFollowOffset(defaults);
		bool useBounds = settings.GetUseBoundaries(defaults);

		var s = settings.UseDefaultBoundaries ? defaults : settings;
		float boundLeft = s.BoundLeft;
		float boundRight = s.BoundRight;
		float boundBottom = s.BoundBottom;
		float boundTop = s.BoundTop;

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size / zoom;
		Vector2 halfView = viewportSize / 2f;

		Vector2 center = Vector2.Zero;

		DrawGizmos(center, halfView, deadzone, followOffset, useBounds, boundLeft, boundRight, boundBottom, boundTop);
	}

	private void DrawGizmos(Vector2 center, Vector2 halfView, Vector2 deadzone, Vector2 followOffset,
		bool useBounds, float boundLeft, float boundRight, float boundBottom, float boundTop)
	{
		if (_drawViewport)
		{
			var rect = new Rect2(center - halfView, halfView * 2f);
			DrawRect(rect, _viewportColor, false, _lineWidth);
		}

		if (_drawBounds && useBounds)
		{
			float godotMinY = -boundTop;
			float godotMaxY = -boundBottom;
			Vector2 boundsMin = new Vector2(boundLeft, godotMinY) - GlobalPosition;
			Vector2 boundsMax = new Vector2(boundRight, godotMaxY) - GlobalPosition;
			DrawRect(new Rect2(boundsMin, boundsMax - boundsMin), _boundsColor, false, _lineWidth);
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
