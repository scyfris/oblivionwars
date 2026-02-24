using Godot;

[GlobalClass]
public partial class NPCBehaviorAttack_Basic : LimboState
{
    [Export] private NPCBehaviorParamsAttack_Basic _settings;

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

        // If player is not in attack range, move towards the player.
        if (_controller.IsPlayerInAttackRange())
        {
            _controller.StopMoving();

            // Update aim
            if (_settings.AimMode == AimMode.TrackPlayer)
            {
                _controller.AimAtPlayer();
            }
            else
            {
                _controller.AimAtFacingDir();
            }
            
            // Shoot player
            _controller.StartShooting();
        } else
        {
            // Move
            _controller.StopShooting();
            _controller.StartMoveTowardsPlayer();
        }
    
        // TODO: Have a "moveandattach" option that denotes behavior of enemy standing in place whlie shooting or running towards player and shooting.
        // TODO: Have a "mindistfromplayer" parameter so npc doesn't try to shove itself into the play lol
    }

    public override void _Exit()
    {
        _controller.StopMoving();
        _controller.StopShooting();
    }
}
