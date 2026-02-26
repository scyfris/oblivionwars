using Godot;

public partial class EffectsSystem : GameSystem
{
    public static EffectsSystem Instance { get; private set; }

    [ExportGroup("Impact Effects")]
    [Export] public PackedScene HitEffectNormal;
    [Export] public PackedScene HitEffectEnemy;

    [ExportGroup("Hit Flash")]
    [Export] public ShaderMaterial HitFlashMaterial;
    [Export] public float HitFlashDuration = 0.15f;
    [Export] public Tween.TransitionType HitFlashTransition = Tween.TransitionType.Linear;
    [Export] public Tween.EaseType HitFlashEase = Tween.EaseType.InOut;

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

        // Impact particle effect
        bool isEnemy = target is NPCEntityCharacterBody2D;
        var scene = isEnemy ? HitEffectEnemy : HitEffectNormal;
        if (scene != null)
        {
            var effect = scene.Instantiate<Node2D>();
            effect.GlobalPosition = evt.HitPosition;
            effect.Rotation = evt.HitDirection.Angle() + Mathf.Pi;
            GetTree().Root.AddChild(effect);
        }

        // White flash on entities
        if (target is EntityCharacterBody2D entity)
            FlashSprite(entity.SpriteNode);
    }

    private void FlashSprite(CanvasItem sprite)
    {
        if (sprite == null || HitFlashMaterial == null) return;

        var mat = (ShaderMaterial)HitFlashMaterial.Duplicate();
        sprite.Material = mat;
        mat.SetShaderParameter("flash_amount", 1.0f);

        var tween = sprite.CreateTween();
        tween.TweenProperty(mat, "shader_parameter/flash_amount", 0.0f, HitFlashDuration)
            .SetTrans(HitFlashTransition)
            .SetEase(HitFlashEase);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(sprite))
                sprite.Material = null;
        }));
    }
}
