using Godot;

/// <summary>
/// Enum identifying the type of saveable level object.
/// Add new types here as you create new saveable objects.
/// </summary>
public enum SaveableLevelObjectType
{
    Unknown = 0,
    Checkpoint = 1,
    Door = 2,
    DestructibleWall = 3,
    Lever = 4,
    Chest = 5,
    Trap = 6,
    QuestItem = 7,
    PuzzleObject = 8,
    // Add more as needed
}
