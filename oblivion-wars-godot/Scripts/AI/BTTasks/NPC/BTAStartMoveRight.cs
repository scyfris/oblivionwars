using Godot;

[GlobalClass]
public partial class BTAStartMoveRight : BTAction
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

        _controller.StartMoveRight();
        return BT.Status.Success;
    }

    public override void _Exit()
    {
        // noop
    }
}
