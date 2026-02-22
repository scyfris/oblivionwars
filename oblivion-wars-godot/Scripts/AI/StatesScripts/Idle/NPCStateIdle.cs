using Godot;

[GlobalClass]
public partial class NPCStateIdle : LimboState
{
    private NPCController _controller;

    public override void _Setup()
    {
        _controller = GetAgent() as NPCController;
    }

    public override void _Enter()
    {
        // TODO: Start idle timer, play idle animation
    }

    public override void _Update(double delta)
    {
        // TODO: Count down idle timer, dispatch "idle_timeout" when done
    }

    public override void _Exit()
    {
    }
}
