using Godot;

public enum NPCTransitionEvent
{
    IdleTimeout,
    PatrolDone,
    PlayerDetected,
    PlayerLost,
    HealthLow,
}

public partial class NPCController : Node, IEntityController
{
    private static StringName Event(NPCTransitionEvent evt) => EnumStringNames<NPCTransitionEvent>.Get(evt);

    [ExportGroup("NPC Definition")]
    [Export] private NPCDefinition _definition;
    public NPCDefinition Definition => _definition;
    CharacterDefinition IEntityController.Definition => _definition;

    private bool IsFlying => _definition?.AIBehaviorData?.IsFlying == true;

    [ExportGroup("Node References")]
    [Export] private NPCEntityCharacterBody2D _npcCharacterBody;
    [Export] private HoldableSystem _holdableSystem;
    [Export] private Label _healthLabel;
    [Export] private Label _stateLabel;
    [Export] private LimboHsm _stateTree;


    [ExportGroup("HSM State Information")]
    [Export] private LimboState _idleState;
    [Export] private LimboState _patrolState;
    [Export] private LimboState _attackState;
    [Export] private LimboState _fleeState;

    // XXX player runtime data ?? should this be shard in NPC?
    protected NPCRuntimeData _runtimeData;
    public NPCRuntimeData NPCRuntimeData => _runtimeData;

    protected virtual void InitializeRuntimeData()
    {
        if (_definition != null)
        {
            // XXX TODO: NEed a player version of this data to differentiate from npc data.
            _runtimeData = new NPCRuntimeData
            {
                EntityId = _definition.EntityId,
                RuntimeInstanceId = GetInstanceId(),
                CurrentHealth = _definition.MaxHealth,
                MaxHealth = _definition.MaxHealth,
            };
        }
    }

    public NPCEntityCharacterBody2D NPCCharacterBody => _npcCharacterBody;

    private PlayerCharacterBody2D _cachedPlayer;
    private bool _isShooting;

    public override void _Ready()
    {
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance.Subscribe<HitEvent>(OnHit);

        InitializeRuntimeData();

        // Initialize holdables
        _holdableSystem?.Initialize(_npcCharacterBody, Definition);

        // Configure flying
        if (_definition?.AIBehaviorData?.IsFlying == true)
            _npcCharacterBody.GravityEnabled = false;

        SetupHSM();
        UpdateHealthLabel();
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Unsubscribe<HitEvent>(OnHit);
    }

    public override void _PhysicsProcess(double delta)
    {
        _holdableSystem?.Update(delta);
        if (_isShooting)
            UseHoldableHeld(true);
        EvaluateTransitions();
    }

    // ── HSM Setup ─────────────────────────────────────────

    private void SetupHSM()
    {
        if (_stateTree == null) return;

        // Idle transitions
        _stateTree.AddTransition(_idleState, _patrolState, Event(NPCTransitionEvent.IdleTimeout));
        _stateTree.AddTransition(_idleState, _attackState, Event(NPCTransitionEvent.PlayerDetected));

        // Patrol transitions
        _stateTree.AddTransition(_patrolState, _idleState, Event(NPCTransitionEvent.PatrolDone));
        _stateTree.AddTransition(_patrolState, _attackState, Event(NPCTransitionEvent.PlayerDetected));

        // Attack transitions
        _stateTree.AddTransition(_attackState, _patrolState, Event(NPCTransitionEvent.PlayerLost));

        // Flee transitions
        _stateTree.AddTransition(_fleeState, _patrolState, Event(NPCTransitionEvent.PlayerLost));

        // Any state can flee
        _stateTree.AddTransition(_stateTree.Anystate(), _fleeState, Event(NPCTransitionEvent.HealthLow));

        _stateTree.ActiveStateChanged += OnActiveStateChanged;

        _stateTree.Initialize(this);
        _stateTree.SetActive(true);
        UpdateStateLabel();
    }

    private void OnActiveStateChanged(LimboState current, LimboState previous)
    {
        UpdateStateLabel();
    }

    private void UpdateStateLabel()
    {
        if (_stateLabel == null) return;
        var active = _stateTree?.GetActiveState();
        _stateLabel.Text = active != null ? active.Name : "";
    }

    private void EvaluateTransitions()
    {
        if (_stateTree == null) return;

        float healthPercent = NPCRuntimeData.MaxHealth > 0
            ? NPCRuntimeData.CurrentHealth / NPCRuntimeData.MaxHealth
            : 1f;

        // Priority order — highest priority first
        if (healthPercent <= _definition.AIBehaviorData.FleeHealthThreshold)
            _stateTree.Dispatch(Event(NPCTransitionEvent.HealthLow));
        else if (_definition.AIBehaviorData.Aggressive && IsPlayerInDetectRange())
            _stateTree.Dispatch(Event(NPCTransitionEvent.PlayerDetected));
        else if (!IsPlayerInDetectRange())
            _stateTree.Dispatch(Event(NPCTransitionEvent.PlayerLost));
    }

    public PlayerCharacterBody2D GetTargetPlayer()
    {
        if (_cachedPlayer == null || !IsInstanceValid(_cachedPlayer))
            _cachedPlayer = GetTree().GetFirstNodeInGroup(GroupConstants.Entities.Player) as PlayerCharacterBody2D;
        return _cachedPlayer;
    }

    // ── Movement ────────────────────────────────────────────

    public void StartMoveLeft() => _npcCharacterBody.StartMoveLeft();
    public void StartMoveRight() => _npcCharacterBody.StartMoveRight();
    public void StopMoving() => _npcCharacterBody.Stop();

    public void StartMoveTowardsPlayer()
    {
        var target = GetTargetPlayer();
        if (target == null)
        {
            StopMoving();
            return;
        }

        float deadzone = _definition.AIBehaviorData.MoveDeadzone;
        float dirX = target.GlobalPosition.X - _npcCharacterBody.GlobalPosition.X;

        if (dirX > deadzone)
            StartMoveRight();
        else if (dirX < -deadzone)
            StartMoveLeft();
        else
            _npcCharacterBody.Stop();

        if (IsFlying)
        {
            float dirY = target.GlobalPosition.Y - _npcCharacterBody.GlobalPosition.Y;

            if (dirY > deadzone)
                _npcCharacterBody.StartMoveDown();
            else if (dirY < -deadzone)
                _npcCharacterBody.StartMoveUp();
            else
                _npcCharacterBody.StopVertical();
        }
    }

    public void StartFleeFromPlayer()
    {
        var target = GetTargetPlayer();
        if (target == null)
        {
            StopMoving();
            return;
        }

        float dirX = target.GlobalPosition.X - _npcCharacterBody.GlobalPosition.X;

        // Move opposite direction from player
        if (dirX > 0f)
            StartMoveLeft();
        else
            StartMoveRight();

        if (IsFlying)
        {
            float dirY = target.GlobalPosition.Y - _npcCharacterBody.GlobalPosition.Y;

            if (dirY > 0f)
                _npcCharacterBody.StartMoveUp();
            else
                _npcCharacterBody.StartMoveDown();
        }
    }

    // ── Detection ───────────────────────────────────────────

    public bool IsPlayerInDetectRange()
    {
        var target = GetTargetPlayer();
        if (target == null) return false;

        float distance = _npcCharacterBody.GlobalPosition.DistanceTo(target.GlobalPosition);
        return distance <= _definition.AIBehaviorData.DetectionRange;
    }

    public bool IsPlayerInAttackRange()
    {
        var target = GetTargetPlayer();
        if (target == null) return false;

        float distance = _npcCharacterBody.GlobalPosition.DistanceTo(target.GlobalPosition);
        return distance <= _definition.AIBehaviorData.AttackRange;
    }

    // ── Combat ─────────────────────────────────────────────

    public void AimAtFacingDir()
    {
        float sign = _npcCharacterBody.IsFacingRight ? 1f : -1f;
        Vector2 origin = _holdableSystem.WeaponGlobalPosition;
        Vector2 aimTarget = origin + _npcCharacterBody.HorizontalDir * sign * 100f;
        UpdateAim(aimTarget);
    }

    public void AimAtPlayer()
    {
        var target = GetTargetPlayer();
        if (target == null) return;

        UpdateAim(target.GlobalPosition);
    }

    public void StartShooting()
    {
        if (!_isShooting)
        {
            _isShooting = true;
            UseHoldablePressed(true);
        }
    }

    public void StopShooting()
    {
        if (_isShooting)
        {
            _isShooting = false;
            UseHoldableReleased(true);
        }
    }

    // ── Holdable API ────────────────────────────────────────

    public void UpdateAim(Vector2 targetPosition)
    {
        _holdableSystem?.UpdateAim(targetPosition);
    }

    public void UseHoldablePressed(bool isLeft)
    {
        if (isLeft) _holdableSystem?.PressLeft();
        else _holdableSystem?.PressRight();
    }

    public void UseHoldableReleased(bool isLeft)
    {
        if (isLeft) _holdableSystem?.ReleaseLeft();
        else _holdableSystem?.ReleaseRight();
    }

    public void UseHoldableHeld(bool isLeft)
    {
        if (isLeft) _holdableSystem?.HeldLeft();
        else _holdableSystem?.HeldRight();
    }

    // ── Event Handlers ───────────────────────────────────

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.TargetInstanceId != _npcCharacterBody.GetInstanceId()) return;

        NPCRuntimeData.CurrentHealth -= evt.FinalDamage;
        if (NPCRuntimeData.CurrentHealth < 0)
            NPCRuntimeData.CurrentHealth = 0;

        UpdateHealthLabel();

        if (NPCRuntimeData.CurrentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Triggers the full death sequence: drops, notifies external systems, then frees the entity.
    /// Can be called internally (health reached 0) or externally (force-kill from spawner, etc.).
    /// </summary>
    public void Die()
    {
        GD.Print($"NPC {_definition?.EntityId ?? "unknown"} died!");
        SpawnDrops();

        EventBus.Instance.Raise(new EntityDiedEvent
        {
            EntityInstanceId = _npcCharacterBody.GetInstanceId(),
            KillerInstanceId = 0,
            Position = _npcCharacterBody.GlobalCenterOfMass
        });

        _npcCharacterBody.QueueFree();
        QueueFree();
    }

    // ── Drops ────────────────────────────────────────────

    private void SpawnDrops()
    {
        var definition = _definition as NPCDefinition;
        if (definition?.DropTable == null) return;

        foreach (var entry in definition.DropTable)
        {
            if (entry?.DropScene == null) continue;
            if (entry.DropChance < 1.0f && GD.Randf() > entry.DropChance) continue;
            if (!string.IsNullOrEmpty(entry.RequiredUnlockId))
            {
                // TODO: Map RequiredUnlockId to AbilityType or ItemType enum and check HasAbility/HasItem
                GD.PrintErr("NPC: RequiredUnlockId needs to be mapped to enum-based system");
                continue;
            }

            int count = (int)GD.RandRange(entry.MinCount, entry.MaxCount + 1);
            for (int i = 0; i < count; i++)
            {
                var pickup = entry.DropScene.Instantiate<Node2D>();
                pickup.GlobalPosition = _npcCharacterBody.GlobalPosition;

                if (pickup is RigidBody2D rb)
                {
                    float impulseX = (float)GD.RandRange(-200.0, 200.0);
                    float impulseY = (float)GD.RandRange(-800.0, -400.0);
                    rb.CallDeferred("apply_impulse", new Vector2(impulseX, impulseY));
                }

                _npcCharacterBody.GetParent().CallDeferred("add_child", pickup);
            }
        }
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel == null || Definition == null) return;
        _healthLabel.Text = $"{NPCRuntimeData.CurrentHealth:F0}/{NPCRuntimeData.MaxHealth:F0}";
    }

    private void OnHit(HitEvent evt)
    {
        if (!GodotObject.IsInstanceValid(_npcCharacterBody) || _npcCharacterBody.IsQueuedForDeletion())
            return;
        if (evt.TargetInstanceId != _npcCharacterBody.GetInstanceId())
            return;

        float finalDamage = evt.BaseDamage;

        // Apply knockback scaled by resistance
        if (evt.ImpactForce > 0 && _definition != null)
        {
            float knockback = evt.ImpactForce * (1f - _definition.KnockbackResistance);
            if (knockback > 0)
                _npcCharacterBody.ApplyKnockback(evt.HitDirection * knockback);
        }

        EventBus.Instance.Raise(new DamageAppliedEvent
        {
            TargetInstanceId = evt.TargetInstanceId,
            FinalDamage = finalDamage,
        });
    }
}
