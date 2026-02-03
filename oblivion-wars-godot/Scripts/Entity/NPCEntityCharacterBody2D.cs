using Godot;

public partial class NPCEntityCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new EnemyDefinition _definition;

    [ExportGroup("Debug")]
    [Export] private Label _healthLabel;

    public override void _Ready()
    {
        base._definition = _definition;
        base._Ready();

        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);

        UpdateHealthLabel();
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.TargetInstanceId != GetInstanceId()) return;

        UpdateHealthLabel();
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.EntityInstanceId != GetInstanceId()) return;

        GD.Print($"NPC {_definition?.EntityId ?? "unknown"} died!");
        QueueFree();
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel == null || _runtimeData == null) return;
        _healthLabel.Text = $"{_runtimeData.CurrentHealth:F0}/{_runtimeData.MaxHealth:F0}";
    }
}
