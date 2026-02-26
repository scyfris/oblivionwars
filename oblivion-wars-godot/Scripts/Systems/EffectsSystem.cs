using Godot;

public partial class EffectsSystem : GameSystem
{
    public static EffectsSystem Instance { get; private set; }

    [ExportGroup("Impact Effects")]
    [Export] public PackedScene HitEffectNormal;
    [Export] public PackedScene HitEffectEnemy;

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("EffectsSystem: Duplicate instance detected, removing this one.");
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
        EventBus.Instance.Subscribe<HitEvent>(OnHitEvent);
    }

    private void OnHitEvent(HitEvent evt)
    {
        var target = GodotObject.InstanceFromId(evt.TargetInstanceId);
        bool isEnemy = target is NPCEntityCharacterBody2D;

        var scene = isEnemy ? HitEffectEnemy : HitEffectNormal;
        if (scene == null) return;

        var effect = scene.Instantiate<Node2D>();
        effect.GlobalPosition = evt.HitPosition;
        effect.Rotation = evt.HitDirection.Angle() + Mathf.Pi;
        GetTree().Root.AddChild(effect);
    }
}
