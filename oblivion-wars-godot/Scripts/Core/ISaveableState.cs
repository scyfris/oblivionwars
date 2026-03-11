using Godot;

/// <summary>
/// Non-generic base interface for polymorphic collections.
/// Allows SaveManager to iterate over all state objects without knowing their specific types.
/// </summary>
public interface ISaveableState
{
    Resource ToSaveData();
    void LoadFromSaveData(Resource data);
    void ResetToDefaults();
}

/// <summary>
/// Generic interface for type-safe save/load operations.
/// Each state class implements this with their specific SaveData type.
/// </summary>
public interface ISaveableState<T> : ISaveableState where T : Resource
{
    new T ToSaveData();
    void LoadFromSaveData(T data);
}
