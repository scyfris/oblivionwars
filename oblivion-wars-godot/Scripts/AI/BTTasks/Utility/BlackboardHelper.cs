using Godot;

public static class BlackboardHelper
{
    /// <summary>
    /// Reads a float from the blackboard. Returns false if the variable doesn't exist and errorIfMissing is true.
    /// </summary>
    public static bool TryReadFloat(Blackboard bb, string key, out float value, bool errorIfMissing = true)
    {
        value = 0f;

        if (!bb.HasVar(key))
        {
            if (errorIfMissing)
                GD.PushError($"BlackboardHelper: Blackboard variable '{key}' not defined in BlackboardPlan.");
            return false;
        }

        value = bb.GetVar(key).AsSingle();
        return true;
    }

    /// <summary>
    /// Writes a float to the blackboard. Returns false if the variable doesn't exist and errorIfMissing is true.
    /// </summary>
    public static bool TryWriteFloat(Blackboard bb, string key, float value, bool errorIfMissing = true)
    {
        if (!bb.HasVar(key))
        {
            if (errorIfMissing)
                GD.PushError($"BlackboardHelper: Blackboard variable '{key}' not defined in BlackboardPlan.");
            return false;
        }

        bb.SetVar(key, value);
        return true;
    }
}
