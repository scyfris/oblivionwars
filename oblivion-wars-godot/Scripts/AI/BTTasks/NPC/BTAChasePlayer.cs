using Godot;

[GlobalClass]
public partial class BTAChasePlayer : BTAction
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

        _controller.StartMoveTowardsPlayer();
        return BT.Status.Success;
    }

    public override void _Exit()
    {
        _controller?.StopMoving();
    }
}
