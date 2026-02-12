using Godot;

/// <summary>
/// Interface for objects that need persistent unique IDs for save/load.
/// Implement this on any Node type (Area2D, RigidBody2D, StaticBody2D, etc.)
/// </summary>
public interface ISaveableObject
{
    /// <summary>
    /// Unique identifier for this object in the level.
    /// Format: "objecttype_levelname_hash" (e.g., "door_MainLevel_A3F2")
    /// </summary>
    string UniqueId { get; set; }

    /// <summary>
    /// The type of this saveable object (e.g., Door, Lever, Chest)
    /// </summary>
    SaveableLevelObjectType GetObjectType();

    /// <summary>
    /// Save the current state of this object to a LevelObjectSaveDataEntry subclass.
    /// Return null if the object has no custom state to save.
    /// </summary>
    LevelObjectSaveDataEntry SaveState();

    /// <summary>
    /// Load and apply saved state from a LevelObjectSaveDataEntry subclass.
    /// Called when the level is loaded if this object has saved data.
    /// </summary>
    void LoadState(LevelObjectSaveDataEntry data);
}

/// <summary>
/// Helper class that provides UniqueId generation for any Node implementing ISaveableObject.
/// Use this as a mixin pattern in your [Tool] scripts.
/// </summary>
public static class SaveableObjectHelper
{
    /// <summary>
    /// Call this in your _Process when _generateUniqueId is true.
    /// </summary>
    public static void GenerateUniqueId(Node node, ISaveableObject saveableObject)
    {
        if (!Engine.IsEditorHint()) return;

        var tree = node.GetTree();
        if (tree == null) return;

        var root = tree.EditedSceneRoot;
        if (root == null)
        {
            GD.PrintErr($"{node.GetType().Name}: Cannot generate ID, no edited scene root");
            return;
        }

        string levelName = root.Name;
        string nodePath = node.GetPath().ToString();
        int hash = nodePath.GetHashCode() & 0xFFFF;
        SaveableLevelObjectType objectType = saveableObject.GetObjectType();
        string objectTypeStr = objectType.ToString().ToLower();

        saveableObject.UniqueId = $"{objectTypeStr}_{levelName}_{hash:X4}";

        GD.Print($"Generated {objectTypeStr} UniqueId: {saveableObject.UniqueId}");
    }

    /// <summary>
    /// Call this in your _Ready when in editor mode to enable processing.
    /// </summary>
    public static void EnableEditorProcessing(Node node)
    {
        if (Engine.IsEditorHint())
        {
            node.SetProcess(true);
        }
    }
}
