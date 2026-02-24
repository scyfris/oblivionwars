using Godot;

/// <summary>
/// Makes all children visible only when debug mode is enabled.
/// Attach to a Control node and parent any debug-only UI under it.
/// </summary>
public partial class DebugModeFilter : Control
{
    public override void _Process(double delta)
    {
        Visible = GlobalStateManager.Instance.Global.IsDebugModeEnabled;
    }
}
