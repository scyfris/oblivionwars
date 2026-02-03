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
        var target = GodotObject.InstanceFromId(evt.TargetInstanceId);
        if (target is not EntityCharacterBody2D entity)
            return;

        float finalDamage = evt.BaseDamage;

        foreach (var effect in entity.RuntimeData.StatusEffects)
        {
            finalDamage *= effect.Definition.DamageMultiplier;
        }

        EventBus.Instance.Raise(new DamageAppliedEvent
        {
            TargetInstanceId = evt.TargetInstanceId,
            FinalDamage = finalDamage,
        });
    }
}
