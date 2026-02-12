using Godot;
using System.Collections.Generic;

#if TOOLS
[Tool]
#endif
public partial class UniqueIdGenerator : Node
{
    [Export] private bool _generateIds = false;

    public override void _Process(double delta)
    {
#if TOOLS
        if (Engine.IsEditorHint() && _generateIds)
        {
            _generateIds = false;
            GenerateUniqueIds();
        }
#endif
    }

    private void GenerateUniqueIds()
    {
        var root = GetTree().EditedSceneRoot;
        if (root == null)
        {
            GD.PrintErr("UniqueIdGenerator: No edited scene root found");
            return;
        }

        string levelName = root.Name;
        int generatedCount = 0;

        // Find all Checkpoints
        var checkpoints = FindNodesOfType<Checkpoint>(root);
        foreach (var checkpoint in checkpoints)
        {
            if (string.IsNullOrEmpty(checkpoint.UniqueId))
            {
                checkpoint.UniqueId = GenerateId("checkpoint", levelName, checkpoint.GetPath());
                generatedCount++;
                GD.Print($"Generated ID for Checkpoint: {checkpoint.UniqueId}");
            }
        }

        // TODO: Add other saveable types here (breakable walls, traps, etc.)

        GD.Print($"UniqueIdGenerator: Generated {generatedCount} unique IDs");

        if (generatedCount > 0)
        {
            GD.Print("IMPORTANT: Save the scene (Ctrl+S) to persist the generated IDs!");
        }
        else
        {
            GD.Print("All objects already have unique IDs");
        }
    }

    private string GenerateId(string typeName, string levelName, NodePath nodePath)
    {
        // Generate a stable hash from the node path
        string pathString = nodePath.ToString();
        int hash = pathString.GetHashCode() & 0xFFFF;  // Last 4 hex digits

        // Format: "checkpoint_MainLevel_A3F2"
        return $"{typeName}_{levelName}_{hash:X4}";
    }

    private List<T> FindNodesOfType<T>(Node root) where T : Node
    {
        var result = new List<T>();
        FindNodesRecursive(root, result);
        return result;
    }

    private void FindNodesRecursive<T>(Node node, List<T> result) where T : Node
    {
        if (node is T typed)
            result.Add(typed);

        foreach (Node child in node.GetChildren())
            FindNodesRecursive(child, result);
    }
}
