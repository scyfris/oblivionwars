using Godot;

public partial class AIController : Node
{
    [Export] private NPCEntityCharacterBody2D _npc;
    [Export] private AIBehaviorDefinition _behavior;
    [Export] private Area2D _detectionArea;

    private enum AIState { Idle, Patrol, Chase, Attack, Returning }

    private AIState _state = AIState.Idle;
    private Vector2 _spawnPosition;
    private Vector2 _patrolTarget;
    private float _idleTimer;
    private float _attackCooldownTimer;
    private PlayerCharacterBody2D _targetPlayer;

    public override void _Ready()
    {
        if (_npc == null || _behavior == null)
        {
            GD.PrintErr("AIController: Missing NPC or behavior definition!");
            return;
        }

        _spawnPosition = _npc.GlobalPosition;

        // Set detection area radius from behavior definition
        if (_detectionArea != null)
        {
            foreach (var child in _detectionArea.GetChildren())
            {
                if (child is CollisionShape2D cs && cs.Shape is CircleShape2D circle)
                {
                    circle.Radius = _behavior.DetectionRadius;
                    break;
                }
            }

            _detectionArea.BodyEntered += OnDetectionBodyEntered;
            _detectionArea.BodyExited += OnDetectionBodyExited;
        }

        _idleTimer = (float)GD.RandRange(_behavior.IdlePauseMin, _behavior.IdlePauseMax);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_npc == null || _behavior == null) return;

        _attackCooldownTimer -= (float)delta;

        switch (_state)
        {
            case AIState.Idle:
                ProcessIdle(delta);
                break;
            case AIState.Patrol:
                ProcessPatrol(delta);
                break;
            case AIState.Chase:
                ProcessChase(delta);
                break;
            case AIState.Attack:
                ProcessAttack(delta);
                break;
            case AIState.Returning:
                ProcessReturning(delta);
                break;
        }

        // Update aim every frame based on AimMode
        UpdateAiming();
    }

    private void ProcessIdle(double delta)
    {
        _npc.Stop();
        _idleTimer -= (float)delta;

        if (_idleTimer <= 0)
        {
            PickPatrolTarget();
            _state = AIState.Patrol;
        }
    }

    private void ProcessPatrol(double delta)
    {
        float distToTarget = _npc.GlobalPosition.DistanceTo(_patrolTarget);

        if (distToTarget < 10f)
        {
            // Arrived at patrol target
            _npc.Stop();
            _idleTimer = (float)GD.RandRange(_behavior.IdlePauseMin, _behavior.IdlePauseMax);
            _state = AIState.Idle;
            return;
        }

        // Walk toward patrol target
        MoveToward(_patrolTarget);

        // If stuck on wall, pick a new target
        if (_npc.IsOnWall())
        {
            PickPatrolTarget();
        }
    }

    private void ProcessChase(double delta)
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
        {
            _state = AIState.Returning;
            return;
        }

        float distToPlayer = _npc.GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);
        float disengageDist = _behavior.DisengageDistance > 0
            ? _behavior.DisengageDistance
            : _behavior.DetectionRadius * 1.5f;

        if (distToPlayer > disengageDist)
        {
            _targetPlayer = null;
            _state = AIState.Returning;
            return;
        }

        if (_behavior.Aggressive)
        {
            // Chase player
            if (distToPlayer <= _behavior.AttackRange && _attackCooldownTimer <= 0)
            {
                _state = AIState.Attack;
                return;
            }
            MoveToward(_targetPlayer.GlobalPosition);
        }
        else
        {
            // Flee from player
            Vector2 fleeDir = (_npc.GlobalPosition - _targetPlayer.GlobalPosition).Normalized();
            Vector2 fleeTarget = _npc.GlobalPosition + fleeDir * 200f;
            MoveToward(fleeTarget);
        }
    }

    private void ProcessAttack(double delta)
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
        {
            _state = AIState.Returning;
            return;
        }

        _npc.Stop();

        // Fire weapon
        Vector2 aimTarget = _targetPlayer.GlobalPosition;
        _npc.UseHoldablePressed(aimTarget, true);

        _attackCooldownTimer = _behavior.AttackCooldown;
        _state = AIState.Chase;
    }

    private void ProcessReturning(double delta)
    {
        float distToSpawn = _npc.GlobalPosition.DistanceTo(_spawnPosition);

        if (distToSpawn < 10f)
        {
            _npc.Stop();
            _idleTimer = (float)GD.RandRange(_behavior.IdlePauseMin, _behavior.IdlePauseMax);
            _state = AIState.Idle;
            return;
        }

        MoveToward(_spawnPosition);
    }

    private void MoveToward(Vector2 target)
    {
        float dir = target.X - _npc.GlobalPosition.X;

        if (dir > 5f)
            _npc.MoveRight();
        else if (dir < -5f)
            _npc.MoveLeft();
        else
            _npc.Stop();
    }

    private void PickPatrolTarget()
    {
        float offsetX = (float)GD.RandRange(-_behavior.PatrolRadius, _behavior.PatrolRadius);
        _patrolTarget = _spawnPosition + new Vector2(offsetX, 0);
    }

    private void UpdateAiming()
    {
        Vector2 aimTarget;

        if (_targetPlayer != null && IsInstanceValid(_targetPlayer) &&
            (_state == AIState.Chase || _state == AIState.Attack))
        {
            switch (_behavior.AimMode)
            {
                case AimMode.TrackPlayer:
                    aimTarget = _targetPlayer.GlobalPosition;
                    break;
                case AimMode.FacingDirection:
                default:
                    float facing = _npc.GlobalPosition.X < _targetPlayer.GlobalPosition.X ? 1f : -1f;
                    aimTarget = _npc.GlobalPosition + new Vector2(facing * 1000f, 0);
                    break;
            }
        }
        else
        {
            // Default: aim in current facing direction
            aimTarget = _npc.GlobalPosition + new Vector2(100f, 0);
        }

        _npc.UpdateAim(aimTarget);
    }

    private void OnDetectionBodyEntered(Node2D body)
    {
        if (body is PlayerCharacterBody2D player)
        {
            _targetPlayer = player;
            if (_state == AIState.Idle || _state == AIState.Patrol || _state == AIState.Returning)
                _state = AIState.Chase;
        }
    }

    private void OnDetectionBodyExited(Node2D body)
    {
        if (body is PlayerCharacterBody2D player && _targetPlayer == player)
        {
            // Don't immediately clear — let disengage distance in ProcessChase handle it
        }
    }
}
