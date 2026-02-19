using Godot;

[GlobalClass]
public partial class BTAMathDiv : BTAction
{
    [Export] private FloatParam _dividend;
    [Export] private FloatParam _divisor;
    [Export] private string _resultKey = "result";

    public override void _Enter()
    {
        // noop
    }

    public override BT.Status _Tick(double delta)
    {
        if (_dividend == null || _divisor == null)
            return BT.Status.Failure;

        var bb = GetBlackboard();

        if (!_dividend.TryResolve(bb, out float dividend))
            return BT.Status.Failure;
        if (!_divisor.TryResolve(bb, out float divisor))
            return BT.Status.Failure;

        if (divisor == 0f)
            return BT.Status.Failure;

        if (!BlackboardHelper.TryWriteFloat(bb, _resultKey, dividend / divisor))
            return BT.Status.Failure;

        return BT.Status.Success;
    }

    public override void _Exit()
    {
        // noop
    }
}
