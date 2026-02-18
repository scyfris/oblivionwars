using System.ComponentModel;
using Godot;

public partial class AIController : Node
{
    [Export] private NPCController _controller;
    [Export] private AIBehaviorDefinition _behavior;
    [Export] private Area2D _detectionArea;
    [Export] public bool _isEnabled = true;

    // TODO: Replace with state machine 
    private enum AIState { Idle, Patrol, Chase, Attack, Returning }

    private AIState _state = AIState.Idle;
    private Vector2 _spawnPosition;
    private Vector2 _patrolTarget;
    private float _idleTimer;
    private float _attackCooldownTimer;
    private PlayerCharacterBody2D _targetPlayer;

    public override void _Ready()
    {
        if (_controller == null || _behavior == null)
        {
            GD.PrintErr("AIController: Missing NPCController or behavior definition!");
            return;
        }

        _spawnPosition = _controller.CharacterBody.GlobalPosition;

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
        if (_isEnabled == false)
        {
            return;
        }
        if (_controller == null || _behavior == null) return;

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

        UpdateAiming();
    }

    private void ProcessIdle(double delta)
    {
        _controller.Stop();
        _idleTimer -= (float)delta;

        if (_idleTimer <= 0)
        {
            PickPatrolTarget();
            _state = AIState.Patrol;
        }
    }

    private void ProcessPatrol(double delta)
    {
        float distToTarget = _controller.CharacterBody.GlobalPosition.DistanceTo(_patrolTarget);

        if (distToTarget < 10f)
        {
            _controller.Stop();
            _idleTimer = (float)GD.RandRange(_behavior.IdlePauseMin, _behavior.IdlePauseMax);
            _state = AIState.Idle;
            return;
        }

        MoveToward(_patrolTarget);

        if (_controller.CharacterBody.IsOnWall())
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

        float distToPlayer = _controller.CharacterBody.GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);
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
            if (distToPlayer <= _behavior.AttackRange && _attackCooldownTimer <= 0)
            {
                _state = AIState.Attack;
                return;
            }
            MoveToward(_targetPlayer.GlobalPosition);
        }
        else
        {
            Vector2 fleeDir = (_controller.CharacterBody.GlobalPosition - _targetPlayer.GlobalPosition).Normalized();
            Vector2 fleeTarget = _controller.CharacterBody.GlobalPosition + fleeDir * 200f;
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

        _controller.Stop();

        Vector2 aimTarget = _targetPlayer.GlobalPosition;
        _controller.UseHoldablePressed(aimTarget, true);

        _attackCooldownTimer = _behavior.AttackCooldown;
        _state = AIState.Chase;
    }

    private void ProcessReturning(double delta)
    {
        float distToSpawn = _controller.CharacterBody.GlobalPosition.DistanceTo(_spawnPosition);

        if (distToSpawn < 10f)
        {
            _controller.Stop();
            _idleTimer = (float)GD.RandRange(_behavior.IdlePauseMin, _behavior.IdlePauseMax);
            _state = AIState.Idle;
            return;
        }

        MoveToward(_spawnPosition);
    }

    private void MoveToward(Vector2 target)
    {
        float dir = target.X - _controller.CharacterBody.GlobalPosition.X;

        if (dir > 5f)
            _controller.MoveRight();
        else if (dir < -5f)
            _controller.MoveLeft();
        else
            _controller.Stop();
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
                    float facing = _controller.CharacterBody.GlobalPosition.X < _targetPlayer.GlobalPosition.X ? 1f : -1f;
                    aimTarget = _controller.CharacterBody.GlobalPosition + new Vector2(facing * 1000f, 0);
                    break;
            }
        }
        else
        {
            aimTarget = _controller.CharacterBody.GlobalPosition + new Vector2(100f, 0);
        }

        _controller.UpdateAim(aimTarget);
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
