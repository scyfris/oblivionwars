using Godot;

public partial class HazardSystem : GameSystem
{
    public static HazardSystem Instance { get; private set; }

    [Export] private HazardDefinition _hazardDefinition;

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("HazardSystem: Duplicate instance detected, removing this one.");
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
        EventBus.Instance.Subscribe<HazardContactEvent>(OnHazardContact);
    }

    private void OnHazardContact(HazardContactEvent evt)
    {
        if (_hazardDefinition == null) return;

        float damage = _hazardDefinition.GetDamage(evt.HazardType);
        if (damage <= 0) return;

        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = evt.EntityInstanceId,
            SourceInstanceId = 0,
            BaseDamage = damage,
            HitDirection = Godot.Vector2.Zero,
            HitPosition = evt.Position,
            Projectile = null
        });
    }
}
