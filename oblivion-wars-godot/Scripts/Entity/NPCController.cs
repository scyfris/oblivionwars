using Godot;

public partial class NPCController : Node
{
    [Export] private NPCEntityCharacterBody2D _characterBody;
    [Export] private HoldableSystem _holdableSystem;
    [Export] private Label _healthLabel;

    [ExportGroup("AIBehavior")]
    [Export] private AIBehaviorDefinition _behavior;

    public NPCEntityCharacterBody2D CharacterBody => _characterBody;

    private PlayerCharacterBody2D _targetPlayer;

    public override void _Ready()
    {
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);

        // Initialize holdables
        if (_holdableSystem != null)
        {
            if (_holdableSystem.UseDefinitionWeapons)
                _holdableSystem.InitializeWithDefinition(_characterBody, _characterBody.Definition);
            else
                _holdableSystem.Initialize(_characterBody);
        }

        // Find and cache player reference
        _targetPlayer = GetTree().GetFirstNodeInGroup(Groups.Entities.Player) as PlayerCharacterBody2D;

        UpdateHealthLabel();
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    public override void _PhysicsProcess(double delta)
    {
        _holdableSystem?.Update(delta);
    }

    // ── Movement ────────────────────────────────────────────

    public void StartMoveLeft() => _characterBody.StartMoveLeft();
    public void StartMoveRight() => _characterBody.StartMoveRight();
    public void StopMoving() => _characterBody.Stop();

    public void StartMoveTowardsPlayer()
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
        {
            StopMoving();
            return;
        }

        float dir = _targetPlayer.GlobalPosition.X - _characterBody.GlobalPosition.X;

        if (dir > 5f)
            StartMoveRight();
        else if (dir < -5f)
            StartMoveLeft();
        else
            StopMoving();
    }

    public void StartFleeFromPlayer()
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
        {
            StopMoving();
            return;
        }

        float dir = _targetPlayer.GlobalPosition.X - _characterBody.GlobalPosition.X;

        // Move opposite direction from player
        if (dir > 0f)
            StartMoveLeft();
        else
            StartMoveRight();
    }

    // ── Detection ───────────────────────────────────────────

    public bool IsPlayerInDetectRange()
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
            return false;

        float distance = _characterBody.GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);
        return distance <= _characterBody.Definition.DetectionRange;
    }

    public bool IsPlayerInAggroRange()
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
            return false;

        float distance = _characterBody.GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);
        return distance <= _characterBody.Definition.AggroRange;
    }

    // ── Combat ─────────────────────────────────────────────

    public void AimAtPlayer()
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
            return;

        UpdateAim(_targetPlayer.GlobalPosition);
    }

    public void ShootAtPlayer()
    {
        if (_targetPlayer == null || !IsInstanceValid(_targetPlayer))
            return;

        UpdateAim(_targetPlayer.GlobalPosition);
        UseHoldablePressed(_targetPlayer.GlobalPosition, true);
    }

    public void StopShooting()
    {
        if (_targetPlayer != null && IsInstanceValid(_targetPlayer))
            UseHoldableReleased(_targetPlayer.GlobalPosition, true);
    }

    // ── Holdable API ────────────────────────────────────────

    public void UpdateAim(Vector2 targetPosition)
    {
        _holdableSystem?.UpdateAim(targetPosition);
    }

    public void UseHoldablePressed(Vector2 target, bool isLeft)
    {
        if (isLeft) _holdableSystem?.PressLeft(target);
        else _holdableSystem?.PressRight(target);
    }

    public void UseHoldableReleased(Vector2 target, bool isLeft)
    {
        if (isLeft) _holdableSystem?.ReleaseLeft(target);
        else _holdableSystem?.ReleaseRight(target);
    }

    public void UseHoldableHeld(Vector2 target, bool isLeft)
    {
        if (isLeft) _holdableSystem?.HeldLeft(target);
        else _holdableSystem?.HeldRight(target);
    }

    // ── Event Handlers ───────────────────────────────────

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.TargetInstanceId != _characterBody.GetInstanceId()) return;
        UpdateHealthLabel();
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.EntityInstanceId != _characterBody.GetInstanceId()) return;

        GD.Print($"NPC {_characterBody.Definition?.EntityId ?? "unknown"} died!");
        SpawnDrops();
        _characterBody.QueueFree();
        QueueFree();
    }

    // ── Drops ────────────────────────────────────────────

    private void SpawnDrops()
    {
        var definition = _characterBody.Definition as EnemyDefinition;
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
                pickup.GlobalPosition = _characterBody.GlobalPosition;

                if (pickup is RigidBody2D rb)
                {
                    float impulseX = (float)GD.RandRange(-200.0, 200.0);
                    float impulseY = (float)GD.RandRange(-800.0, -400.0);
                    rb.CallDeferred("apply_impulse", new Vector2(impulseX, impulseY));
                }

                _characterBody.GetParent().CallDeferred("add_child", pickup);
            }
        }
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel == null || _characterBody.RuntimeData == null) return;
        _healthLabel.Text = $"{_characterBody.RuntimeData.CurrentHealth:F0}/{_characterBody.RuntimeData.MaxHealth:F0}";
    }
}
