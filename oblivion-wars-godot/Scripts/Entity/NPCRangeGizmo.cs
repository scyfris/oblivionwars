using Godot;

[Tool]
public partial class NPCRangeGizmo : Node2D
{
    private const string DefinitionProperty = "_definition";
    private const string DetectionRangeProperty = "DetectionRange";
    private const string AggroRangeProperty = "AggroRange";

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;

        var parent = GetParent();
        if (parent == null) return;

        // Non-tool C# types aren't available in editor, so read via Get()
        var defVariant = parent.Get(DefinitionProperty);
        if (defVariant.VariantType == Variant.Type.Nil)
        {
            GD.PrintErr($"NPCRangeGizmo: Parent '{parent.Name}' has no '{DefinitionProperty}' property.");
            return;
        }

        var definition = defVariant.AsGodotObject();
        if (definition == null) return;

        var detectionVariant = definition.Get(DetectionRangeProperty);
        var aggroVariant = definition.Get(AggroRangeProperty);
        if (detectionVariant.VariantType == Variant.Type.Nil || aggroVariant.VariantType == Variant.Type.Nil)
            return;

        float detectionRange = detectionVariant.AsSingle();
        float aggroRange = aggroVariant.AsSingle();

        // Yellow circle = DetectionRange
        DrawArc(Vector2.Zero, detectionRange, 0, Mathf.Tau, 128,
                new Color(1f, 1f, 0f, 0.8f), 3f);

        // Red circle = AggroRange
        DrawArc(Vector2.Zero, aggroRange, 0, Mathf.Tau, 128,
                new Color(1f, 0.2f, 0.2f, 0.8f), 3f);
    }
}
