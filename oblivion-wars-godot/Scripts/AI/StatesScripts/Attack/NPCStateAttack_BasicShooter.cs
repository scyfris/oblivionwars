using Godot;

[GlobalClass]
public partial class NPCStateAttack_BasicShooter : LimboState
{
    [Export] private AttackSettings _settings;

    private NPCController _controller;

    public override void _Setup()
    {
        _controller = GetAgent() as NPCController;
    }

    public override void _Enter()
    {
        // TODO: Start aiming at player, begin attack cooldown
    }

    public override void _Update(double delta)
    {
        // TODO: Aim at player, shoot on cooldown, chase if needed
    }

    public override void _Exit()
    {
        _controller.StopShooting();
    }
}
