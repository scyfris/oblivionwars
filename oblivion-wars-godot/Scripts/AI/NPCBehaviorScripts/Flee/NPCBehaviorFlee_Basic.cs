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
    }

    public override void _Update(double delta)
    {
        // TODO: Figure out how much logic you want to push in here.  Really 
        // start flee from player - this should probably all be defined in here...
        _controller.StartFleeFromPlayer();
    }

    public override void _Exit()
    {
        _controller.StopMoving();
    }
}
