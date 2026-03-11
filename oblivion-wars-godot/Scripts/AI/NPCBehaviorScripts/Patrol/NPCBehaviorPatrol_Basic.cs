using Godot;

[GlobalClass]
public partial class NPCBehaviorPatrol_Basic : LimboState
{
    [Export] public NPCBehaviorParamsPatrol_Basic _settings;

    private NPCController _controller;
    private double _timer;
    private bool _movingRight;

    // secs in each direction, should come from params
    private float dirTime = 1.0f;

    public override void _Setup()
    {
        _controller = GetAgent() as NPCController;
    }

    public override void _Enter()
    {
        _timer = 0.0;
        _movingRight = true;
        _controller.StartMoveRight();
    }

    public override void _Update(double delta)
    {
        _timer += delta;
        if (_timer >= dirTime)
        {
            _timer = 0.0;
            _movingRight = !_movingRight;
            if (_movingRight)
                _controller.StartMoveRight();
            else
                _controller.StartMoveLeft();
        }
    }

    public override void _Exit()
    {
        _controller.StopMoving();
    }
}
