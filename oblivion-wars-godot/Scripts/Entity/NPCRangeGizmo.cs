using Godot;

[Tool]
public partial class NPCRangeGizmo : Node2D
{
    private const string DefinitionProperty = "_definition";
    private const string AIBehaviorDataProperty = "AIBehaviorData";
    private const string DetectionRangeProperty = "DetectionRange";
    private const string AttackRangeProperty = "AttackRange";

    [Export] private NodePath _npcControllerPath;

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

        if (parent is not Node2D)
        {
            GD.PrintErr($"NPCRangeGizmo: Must be a child of a Node2D, but parent '{parent.Name}' is a {parent.GetClass()}.");
            return;
        }

        // Resolve the NPC controller via exported NodePath
        if (_npcControllerPath == null || _npcControllerPath.IsEmpty)
        {
            GD.PrintErr("NPCRangeGizmo: _npcControllerPath is not set.");
            return;
        }
        var controller = GetNode(_npcControllerPath);
        if (controller == null)
        {
            GD.PrintErr($"NPCRangeGizmo: Could not find node at path '{_npcControllerPath}'.");
            return;
        }

        // Non-tool C# types aren't available in editor, so read via Get()
        var defVariant = controller.Get(DefinitionProperty);
        if (defVariant.VariantType == Variant.Type.Nil)
        {
            GD.PrintErr($"NPCRangeGizmo: '{controller.Name}' has no '{DefinitionProperty}' property.");
            return;
        }

        var definition = defVariant.AsGodotObject();
        if (definition == null) return;

        var aiVariant = definition.Get(AIBehaviorDataProperty);
        if (aiVariant.VariantType == Variant.Type.Nil)
        {
            GD.PrintErr($"NPCRangeGizmo: Definition has no '{AIBehaviorDataProperty}' property.");
            return;
        }

        var aiParams = aiVariant.AsGodotObject();
        if (aiParams == null) return;

        var detectionVariant = aiParams.Get(DetectionRangeProperty);
        if (detectionVariant.VariantType == Variant.Type.Nil)
        {
            GD.PrintErr($"NPCRangeGizmo: AIBehaviorData has no '{DetectionRangeProperty}' property.");
            return;
        }

        var attackVariant = aiParams.Get(AttackRangeProperty);
        if (attackVariant.VariantType == Variant.Type.Nil)
        {
            GD.PrintErr($"NPCRangeGizmo: AIBehaviorData has no '{AttackRangeProperty}' property.");
            return;
        }

        float detectionRange = detectionVariant.AsSingle();
        float attackRange = attackVariant.AsSingle();

        // Yellow circle = DetectionRange
        DrawArc(Vector2.Zero, detectionRange, 0, Mathf.Tau, 128,
                new Color(1f, 1f, 0f, 0.8f), 3f);

        // Red circle = AttackRange
        DrawArc(Vector2.Zero, attackRange, 0, Mathf.Tau, 128,
                new Color(1f, 0.2f, 0.2f, 0.8f), 3f);
    }
}
