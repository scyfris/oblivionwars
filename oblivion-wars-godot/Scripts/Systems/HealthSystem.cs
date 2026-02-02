using Godot;

public partial class HealthSystem : GameSystem
{
    public static HealthSystem Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("HealthSystem: Duplicate instance detected, removing this one.");
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
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.RemainingHealth <= 0)
        {
            EventBus.Instance.Raise(new EntityDiedEvent
            {
                EntityInstanceId = evt.TargetInstanceId,
                KillerInstanceId = 0
            });
        }
    }
}
