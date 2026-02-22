using Godot;

[GlobalClass]
public partial class NPCBehaviorPatrol_Basic : LimboState
{
    [Export] public NPCBehaviorParamsPatrol_Basic _settings;

    private NPCController _controller;

    public override void _Setup()
    {
        _controller = GetAgent() as NPCController;
    }

    public override void _Enter()
    {
        // TODO: Pick patrol direction, start walking
    }

    public override void _Update(double delta)
    {
        // TODO: Walk back and forth within PatrolRadius, pause at endpoints
    }

    public override void _Exit()
    {
        _controller.StopMoving();
    }
}
