using Godot;

[GlobalClass]
public partial class NPCBehaviorFlee_Basic : LimboState
{
    [Export] private NPCBehaviorParamsFlee_Basic _settings;

    private NPCController _controller;

    public override void _Setup()
    {
        _controller = GetAgent() as NPCController;
    }

    public override void _Enter()
    {
        // TODO: Start fleeing from player
    }

    public override void _Update(double delta)
    {
        // TODO: Move away from player
    }

    public override void _Exit()
    {
        _controller.StopMoving();
    }
}
