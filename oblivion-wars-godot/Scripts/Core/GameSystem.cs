using Godot;

public abstract partial class GameSystem : Node
{
    public override void _Ready()
    {
        Initialize();
    }

    /// <summary>
    /// Called after _Ready. Subscribe to events here via EventBus.Instance.
    /// </summary>
    protected abstract void Initialize();
}
