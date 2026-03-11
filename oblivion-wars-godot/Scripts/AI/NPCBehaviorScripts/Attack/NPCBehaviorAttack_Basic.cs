using Godot;

[GlobalClass]
public partial class NPCBehaviorAttack_Basic : LimboState
{
    [Export] private NPCBehaviorParamsAttack_Basic _settings;

    private NPCController _controller;
    private float _cooldownTimer;
    private bool _isShooting;

    public override void _Setup()
    {
        _controller = GetAgent() as NPCController;
    }

    public override void _Enter()
    {
        _cooldownTimer = 0f;
        _isShooting = false;
    }

    public override void _Update(double delta)
    {
        bool inAttackRange = _controller.IsPlayerInAttackRange();

        // Always aim
        if (_settings.AimMode == AimMode.TrackPlayer)
            _controller.AimAtPlayer();
        else
            _controller.AimAtFacingDir();

        if (inAttackRange)
        {
            // Attack timing: shoot for AttackDuration, pause for AttackCooldown, repeat
            _cooldownTimer -= (float)delta;
            if (!_isShooting)
            {
                // Waiting between bursts — optionally move toward player
                if (_settings.MoveTowardsPlayerDuringCooldown)
                    _controller.StartMoveTowardsPlayer();
                else
                    _controller.StopMoving();

                if (_cooldownTimer <= 0f)
                {
                    _controller.StartShooting();
                    _isShooting = true;
                    _cooldownTimer = _settings.AttackDuration;
                }
            }
            else
            {
                // Shooting — optionally move while attacking
                if (_settings.MoveWhileAttacking)
                    _controller.StartMoveTowardsPlayer();
                else
                    _controller.StopMoving();

                if (_cooldownTimer <= 0f)
                {
                    _controller.StopShooting();
                    _isShooting = false;
                    _cooldownTimer = _settings.AttackCooldown;
                }
            }
        }
        else
        {
            // Out of attack range — finish current burst, then chase
            _cooldownTimer -= (float)delta;
            if (_isShooting)
            {
                if (_settings.MoveWhileAttacking)
                    _controller.StartMoveTowardsPlayer();
                else
                    _controller.StopMoving();

                if (_cooldownTimer <= 0f)
                {
                    _controller.StopShooting();
                    _isShooting = false;
                    _cooldownTimer = _settings.AttackCooldown;
                }
            }
            else
            {
                _controller.StartMoveTowardsPlayer();
            }
        }
    }

    public override void _Exit()
    {
        _controller.StopMoving();
        _controller.StopShooting();
        _isShooting = false;
    }
}
