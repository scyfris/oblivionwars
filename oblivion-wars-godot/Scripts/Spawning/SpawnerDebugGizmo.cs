using Godot;

/// <summary>
/// Debug gizmo for EnemySpawner. Draws activation range circle, spawn point markers,
/// and alive/total count. Visible in editor and at runtime when debug mode is enabled.
/// </summary>
[Tool]
public partial class SpawnerDebugGizmo : Node2D
{
    private const string DefinitionProperty = "_definition";
    private const string RequirePlayerProximityProperty = "RequirePlayerProximity";
    private const string ActivationRangeProperty = "ActivationRange";

    [Export] private Color _activationRangeColor = new(1f, 1f, 0f, 0.5f);
    [Export] private Color _spawnPointColor = new(0f, 1f, 0.4f, 0.8f);
    [Export] private Color _customTableColor = new(0.3f, 0.6f, 1f, 0.8f);
    [Export] private Color _spawnerCenterColor = new(1f, 0.3f, 0.3f, 0.9f);
    [Export] private Color _textColor = new(1f, 1f, 1f, 0.9f);
    [Export] private float _lineWidth = 2f;
    [Export] private float _spawnPointSize = 8f;

    private bool _wasDebugActive;

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            QueueRedraw();
            return;
        }

        var global = GlobalStateManager.Instance?.Global;
        bool debugActive = global?.IsDebugModeEnabled == true && global?.ShowSpawnerGizmo == true;

        if (debugActive)
            QueueRedraw();
        else if (_wasDebugActive)
            QueueRedraw();

        _wasDebugActive = debugActive;
    }

    public override void _Draw()
    {
        if (Engine.IsEditorHint())
            DrawEditor();
        else
        {
            var global = GlobalStateManager.Instance?.Global;
            if (global?.IsDebugModeEnabled == true && global?.ShowSpawnerGizmo == true)
                DrawRuntime();
        }
    }

    private void DrawEditor()
    {
        var parent = GetParent();
        if (parent == null) return;

        // Read definition via Get() since non-tool C# types aren't available in editor
        var defVariant = parent.Get(DefinitionProperty);
        if (defVariant.VariantType == Variant.Type.Nil) return;

        var definition = defVariant.AsGodotObject();
        if (definition == null) return;

        var proximityVariant = definition.Get(RequirePlayerProximityProperty);
        bool requireProximity = proximityVariant.VariantType != Variant.Type.Nil && proximityVariant.AsBool();

        float activationRange = 0f;
        if (requireProximity)
        {
            var rangeVariant = definition.Get(ActivationRangeProperty);
            if (rangeVariant.VariantType != Variant.Type.Nil)
                activationRange = rangeVariant.AsSingle();
        }

        DrawGizmos(activationRange, requireProximity, -1, -1);
    }

    private void DrawRuntime()
    {
        var parent = GetParent();
        if (parent is not EnemySpawner spawner) return;

        // Read definition via the public field
        var def = spawner._definition;
        if (def == null) return;

        float activationRange = def.RequirePlayerProximity ? def.ActivationRange : 0f;

        DrawGizmos(activationRange, def.RequirePlayerProximity, spawner.AliveCount, def.MaxConcurrent);
    }

    private void DrawGizmos(float activationRange, bool requireProximity, int aliveCount, int maxConcurrent)
    {
        // Spawner center cross
        float crossSize = 12f;
        DrawLine(new Vector2(-crossSize, 0), new Vector2(crossSize, 0), _spawnerCenterColor, _lineWidth + 1);
        DrawLine(new Vector2(0, -crossSize), new Vector2(0, crossSize), _spawnerCenterColor, _lineWidth + 1);

        // Activation range circle
        if (requireProximity && activationRange > 0)
        {
            DrawArc(Vector2.Zero, activationRange, 0, Mathf.Tau, 128, _activationRangeColor, _lineWidth);
        }

        // Spawn point markers (child Marker2D nodes of parent)
        var parent = GetParent();
        if (parent != null)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child == this) continue;

                if (Engine.IsEditorHint())
                {
                    // In editor, use Godot class checks since C# types may not resolve
                    if (child is Marker2D marker)
                    {
                        Vector2 localPos = marker.Position;
                        // Check if it has an EnemyTable property (SpawnPoint)
                        var tableVar = child.Get("EnemyTable");
                        bool hasCustomTable = tableVar.VariantType != Variant.Type.Nil
                            && tableVar.AsGodotArray()?.Count > 0;
                        DrawSpawnPointMarker(localPos, hasCustomTable ? _customTableColor : _spawnPointColor);
                    }
                }
                else
                {
                    if (child is SpawnPoint sp)
                    {
                        Vector2 localPos = sp.Position;
                        DrawSpawnPointMarker(localPos, sp.HasCustomTable ? _customTableColor : _spawnPointColor);
                    }
                    else if (child is Marker2D marker)
                    {
                        Vector2 localPos = marker.Position;
                        DrawSpawnPointMarker(localPos, _spawnPointColor);
                    }
                }
            }
        }

        // Alive count text (runtime only)
        if (aliveCount >= 0)
        {
            string text = $"Alive: {aliveCount}/{maxConcurrent}";
            DrawString(ThemeDB.FallbackFont, new Vector2(-40, -20), text, HorizontalAlignment.Left, -1, 14, _textColor);
        }
    }

    private void DrawSpawnPointMarker(Vector2 pos, Color color)
    {
        // Diamond shape
        float s = _spawnPointSize;
        var points = new Vector2[]
        {
            pos + new Vector2(0, -s),
            pos + new Vector2(s, 0),
            pos + new Vector2(0, s),
            pos + new Vector2(-s, 0),
        };

        for (int i = 0; i < points.Length; i++)
            DrawLine(points[i], points[(i + 1) % points.Length], color, _lineWidth);

        // Small center dot
        DrawCircle(pos, 2f, color);
    }
}
