using Godot;

/// <summary>
/// Draws semi-transparent tile grid lines over the visible viewport area.
/// At runtime: only draws when debug mode + ShowTileGrid are enabled.
/// In the editor: always draws around the gizmo's position.
/// </summary>
[Tool]
public partial class TileGridDebugGizmo : Node2D
{
    [Export] private int _tileSize = 64;
    [Export] private Color _gridColor = new(1f, 1f, 1f, 0.15f);
    [Export] private float _lineWidth = 1f;

    /// <summary>How many extra tiles beyond the viewport edge to draw (avoids pop-in).</summary>
    [Export] private int _padding = 2;

    private bool _wasDebugActive;

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            QueueRedraw();
            return;
        }

        var global = GlobalStateManager.Instance?.Global;
        bool debugActive = global?.IsDebugModeEnabled == true && global?.ShowTileGrid == true;

        if (debugActive)
        {
            var cam = CameraController.Instance?.GetCamera();
            if (cam != null)
                GlobalPosition = cam.GlobalPosition;
            QueueRedraw();
        }
        else if (_wasDebugActive)
        {
            QueueRedraw();
        }

        _wasDebugActive = debugActive;
    }

    public override void _Draw()
    {
        if (Engine.IsEditorHint())
        {
            DrawGrid();
        }
        else
        {
            var global = GlobalStateManager.Instance?.Global;
            if (global?.IsDebugModeEnabled == true && global?.ShowTileGrid == true)
                DrawGrid();
        }
    }

    private void DrawGrid()
    {
        if (_tileSize <= 0) return;

        Vector2 viewportSize;
        if (Engine.IsEditorHint())
        {
            int viewW = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
            int viewH = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
            viewportSize = new Vector2(viewW, viewH);
        }
        else
        {
            viewportSize = GetViewport().GetVisibleRect().Size;
            var cam = CameraController.Instance?.GetCamera();
            if (cam != null)
                viewportSize /= cam.Zoom;
        }

        float halfW = viewportSize.X / 2f + _padding * _tileSize;
        float halfH = viewportSize.Y / 2f + _padding * _tileSize;

        // Snap grid origin to tile boundaries relative to world origin
        // GlobalPosition is set to camera position; grid lines should align to world tile grid
        float worldX = GlobalPosition.X;
        float worldY = GlobalPosition.Y;

        float startWorldX = Mathf.Floor((worldX - halfW) / _tileSize) * _tileSize;
        float endWorldX = Mathf.Ceil((worldX + halfW) / _tileSize) * _tileSize;
        float startWorldY = Mathf.Floor((worldY - halfH) / _tileSize) * _tileSize;
        float endWorldY = Mathf.Ceil((worldY + halfH) / _tileSize) * _tileSize;

        // Convert to local coords (since _Draw is in local space and our position == camera position)
        for (float wx = startWorldX; wx <= endWorldX; wx += _tileSize)
        {
            float localX = wx - worldX;
            DrawLine(
                new Vector2(localX, -halfH),
                new Vector2(localX, halfH),
                _gridColor, _lineWidth);
        }

        for (float wy = startWorldY; wy <= endWorldY; wy += _tileSize)
        {
            float localY = wy - worldY;
            DrawLine(
                new Vector2(-halfW, localY),
                new Vector2(halfW, localY),
                _gridColor, _lineWidth);
        }
    }
}
