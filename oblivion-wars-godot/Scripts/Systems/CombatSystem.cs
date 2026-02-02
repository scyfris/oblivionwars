using Godot;

public partial class CombatSystem : GameSystem
{
    public static CombatSystem Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("CombatSystem: Duplicate instance detected, removing this one.");
            QueueFree();
            return;
        }
        Instance = this;
        base._Ready();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    protected override void Initialize()
    {
        EventBus.Instance.Subscribe<HitEvent>(OnHit);
    }

    private void OnHit(HitEvent evt)
    {
        var target = GodotObject.InstanceFromId(evt.TargetInstanceId) as Node2D;
        if (target is not IGameEntity entity)
            return;

        float finalDamage = evt.BaseDamage;

        // Apply status effect modifiers (e.g., vulnerable increases damage taken)
        foreach (var effect in entity.RuntimeData.StatusEffects)
        {
            finalDamage *= effect.Definition.DamageMultiplier;
        }

        // Apply knockback resistance from definition
        // (knockback itself is applied directly to the physics body by the caller if needed)

        entity.RuntimeData.CurrentHealth -= finalDamage;
        if (entity.RuntimeData.CurrentHealth < 0)
            entity.RuntimeData.CurrentHealth = 0;

        EventBus.Instance.Raise(new DamageAppliedEvent
        {
            TargetInstanceId = evt.TargetInstanceId,
            FinalDamage = finalDamage,
            RemainingHealth = entity.RuntimeData.CurrentHealth
        });
    }
}
