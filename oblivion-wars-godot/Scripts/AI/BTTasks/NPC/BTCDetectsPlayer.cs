using Godot;

[GlobalClass]
public partial class BTCDetectsPlayer : BTCondition
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

        return _controller.DetectsPlayer() ? BT.Status.Success : BT.Status.Failure;
    }

    public override void _Exit()
    {
        // noop
    }
}
