using Godot;

[GlobalClass]
public partial class BTAWriteHealthToBlackboard : BTAction
{
    [Export] private string _healthKey = "health";
    [Export] private string _maxHealthKey = "max_health";

    private NPCController _controller;

    public override void _Enter()
    {
        _controller = GetAgent() as NPCController;
    }

    public override BT.Status _Tick(double delta)
    {
        if (_controller == null)
            return BT.Status.Failure;

        var runtimeData = _controller.NPCRuntimeData;
        if (runtimeData == null)
            return BT.Status.Failure;

        var bb = GetBlackboard();

        if (!BlackboardHelper.TryWriteFloat(bb, _healthKey, runtimeData.CurrentHealth))
            return BT.Status.Failure;
        if (!BlackboardHelper.TryWriteFloat(bb, _maxHealthKey, runtimeData.MaxHealth))
            return BT.Status.Failure;

        return BT.Status.Success;
    }

    public override void _Exit()
    {
        // noop
    }
}
