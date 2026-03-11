using Godot;

[GlobalClass]
public partial class FloatParam : Resource
{
    public enum Mode { DirectValue, Blackboard }

    [Export] public Mode Source = Mode.DirectValue;
    [Export] public float DirectValue = 0f;
    [Export] public string BlackboardKey = "";

    public bool TryResolve(Blackboard bb, out float value, bool errorIfMissing = true)
    {
        if (Source == Mode.Blackboard)
            return BlackboardHelper.TryReadFloat(bb, BlackboardKey, out value, errorIfMissing);

        value = DirectValue;
        return true;
    }
}
