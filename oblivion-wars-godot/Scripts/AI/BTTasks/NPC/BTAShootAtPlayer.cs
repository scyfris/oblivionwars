using Godot;

[GlobalClass]
public partial class BTAShootAtPlayer : BTAction
{
    private NPCController _controller;

    public override void _Enter()
    {
        _controller = GetAgent() as NPCController;
    }

    public override BT.Status _Tick(double delta)
    {
        if (_controller == null)
            return BT.Status.Failure;

        _controller.ShootAtPlayer();
        return BT.Status.Running;
    }

    public override void _Exit()
    {
        _controller?.StopShooting();
    }
}
