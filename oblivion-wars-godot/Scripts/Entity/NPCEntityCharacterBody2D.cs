using Godot;

public partial class NPCEntityCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new EnemyDefinition _definition;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;

    [ExportGroup("Combat")]
    [Export] private HoldableSystem _holdableSystem;

    [ExportGroup("Debug")]
    [Export] private Label _healthLabel;

    private Vector2 _aimTarget;
    private bool _facingRight = true;

    public new EnemyDefinition Definition => _definition;

    public override void _Ready()
    {
        base._definition = _definition;
        base._Ready();

        _holdableSystem?.Initialize(this);

        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);

        UpdateHealthLabel();
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        UpdateFacing();
        _holdableSystem?.Update(delta);
    }

    // ── Aim / Holdable API (mirrors PlayerCharacterBody2D) ──

    public void UpdateAim(Vector2 targetPosition)
    {
        _aimTarget = targetPosition;
        _holdableSystem?.UpdateAim(targetPosition);
    }

    public void UseHoldablePressed(Vector2 targetPosition, bool isLeft)
    {
        if (isLeft)
            _holdableSystem?.PressLeft(targetPosition);
        else
            _holdableSystem?.PressRight(targetPosition);
    }

    public void UseHoldableReleased(Vector2 targetPosition, bool isLeft)
    {
        if (isLeft)
            _holdableSystem?.ReleaseLeft(targetPosition);
        else
            _holdableSystem?.ReleaseRight(targetPosition);
    }

    public void UseHoldableHeld(Vector2 targetPosition, bool isLeft)
    {
        if (isLeft)
            _holdableSystem?.HeldLeft(targetPosition);
        else
            _holdableSystem?.HeldRight(targetPosition);
    }

    // ── Visuals ────────────────────────────────────────────

    private void UpdateFacing()
    {
        if (_flipRoot == null) return;

        // Face based on aim target when chasing, otherwise face movement direction
        if (_moveDirection != 0)
            _facingRight = _moveDirection > 0;

        _flipRoot.Scale = new Vector2(_facingRight ? 1 : -1, 1);
    }

    // ── Events ─────────────────────────────────────────────

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.TargetInstanceId != GetInstanceId()) return;
        UpdateHealthLabel();
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.EntityInstanceId != GetInstanceId()) return;

        GD.Print($"NPC {_definition?.EntityId ?? "unknown"} died!");
        SpawnDrops();
        QueueFree();
    }

    private void SpawnDrops()
    {
        if (_definition?.DropTable == null) return;

        foreach (var entry in _definition.DropTable)
        {
            if (entry?.DropScene == null) continue;
            if (entry.DropChance < 1.0f && GD.Randf() > entry.DropChance) continue;
            if (!string.IsNullOrEmpty(entry.RequiredUnlockId) &&
                (PlayerState.Instance == null || !PlayerState.Instance.HasUnlock(entry.RequiredUnlockId)))
                continue;

            int count = (int)GD.RandRange(entry.MinCount, entry.MaxCount + 1);
            for (int i = 0; i < count; i++)
            {
                var pickup = entry.DropScene.Instantiate<Node2D>();
                pickup.GlobalPosition = GlobalPosition;

                // Apply random upward impulse for pop-out effect
                if (pickup is RigidBody2D rb)
                {
                    float impulseX = (float)GD.RandRange(-100.0, 100.0);
                    float impulseY = (float)GD.RandRange(-200.0, -100.0);
                    // Defer impulse to after it's in the tree
                    rb.CallDeferred("apply_impulse", new Vector2(impulseX, impulseY));
                }

                GetParent().CallDeferred("add_child", pickup);
            }
        }
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel == null || _runtimeData == null) return;
        _healthLabel.Text = $"{_runtimeData.CurrentHealth:F0}/{_runtimeData.MaxHealth:F0}";
    }
}
