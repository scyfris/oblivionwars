using Godot;
using System.Collections.Generic;

public partial class StatusEffectSystem : GameSystem
{
    public static StatusEffectSystem Instance { get; private set; }

    private Dictionary<string, StatusEffectDefinition> _registry = new();

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("StatusEffectSystem: Duplicate instance detected, removing this one.");
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
        LoadDefinitions();
    }

    private void LoadDefinitions()
    {
        string path = "res://Resources/Data/StatusEffects";
        if (!DirAccess.DirExistsAbsolute(path))
            return;

        using var dir = DirAccess.Open(path);
        if (dir == null)
            return;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
            {
                var def = ResourceLoader.Load<StatusEffectDefinition>($"{path}/{fileName}");
                if (def != null)
                {
                    if (_registry.ContainsKey(def.EffectId))
                    {
                        GD.PrintErr($"StatusEffectSystem: Duplicate EffectId '{def.EffectId}' in {fileName}");
                    }
                    else
                    {
                        _registry[def.EffectId] = def;
                    }
                }
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        GD.Print($"StatusEffectSystem: Loaded {_registry.Count} status effect definitions.");
    }

    public override void _PhysicsProcess(double delta)
    {
        // StatusEffects live on each entity's RuntimeData.
        // Systems that own entities should call TickEffects on their entities,
        // or we can iterate registered entities here in the future.
    }

    public void ApplyEffect(IGameEntity entity, string effectId, float duration = -1f)
    {
        if (!_registry.TryGetValue(effectId, out var def))
        {
            GD.PrintErr($"StatusEffectSystem: Unknown effect '{effectId}'");
            return;
        }

        var existing = entity.RuntimeData.StatusEffects.Find(e => e.EffectId == effectId);
        if (existing != null)
        {
            if (def.Stackable && existing.CurrentStacks < def.MaxStacks)
            {
                existing.CurrentStacks++;
                existing.RemainingDuration = duration >= 0 ? duration : def.DefaultDuration;
            }
            else
            {
                // Refresh duration
                existing.RemainingDuration = duration >= 0 ? duration : def.DefaultDuration;
            }
            return;
        }

        entity.RuntimeData.StatusEffects.Add(new ActiveStatusEffect
        {
            EffectId = effectId,
            RemainingDuration = duration >= 0 ? duration : def.DefaultDuration,
            TickTimer = def.TickInterval,
            CurrentStacks = 1,
            Definition = def
        });

        EventBus.Instance.Raise(new StatusEffectAppliedEvent
        {
            TargetInstanceId = entity.RuntimeData.RuntimeInstanceId,
            EffectId = effectId
        });
    }

    public void RemoveEffect(IGameEntity entity, string effectId)
    {
        var idx = entity.RuntimeData.StatusEffects.FindIndex(e => e.EffectId == effectId);
        if (idx >= 0)
        {
            entity.RuntimeData.StatusEffects.RemoveAt(idx);
            EventBus.Instance.Raise(new StatusEffectRemovedEvent
            {
                TargetInstanceId = entity.RuntimeData.RuntimeInstanceId,
                EffectId = effectId
            });
        }
    }

    public bool HasEffect(IGameEntity entity, string effectId)
    {
        return entity.RuntimeData.StatusEffects.Exists(e => e.EffectId == effectId);
    }

    /// <summary>
    /// Tick all status effects on an entity. Call this from the entity's _PhysicsProcess or from a managing system.
    /// </summary>
    public void TickEffects(IGameEntity entity, float delta)
    {
        for (int i = entity.RuntimeData.StatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = entity.RuntimeData.StatusEffects[i];
            effect.RemainingDuration -= delta;

            // Tick damage
            if (effect.Definition.TickInterval > 0 && effect.Definition.TickDamage > 0)
            {
                effect.TickTimer -= delta;
                if (effect.TickTimer <= 0)
                {
                    effect.TickTimer += effect.Definition.TickInterval;
                    entity.RuntimeData.CurrentHealth -= effect.Definition.TickDamage * effect.CurrentStacks;
                    if (entity.RuntimeData.CurrentHealth < 0)
                        entity.RuntimeData.CurrentHealth = 0;
                }
            }

            // Remove expired
            if (effect.RemainingDuration <= 0)
            {
                string removedId = effect.EffectId;
                entity.RuntimeData.StatusEffects.RemoveAt(i);
                EventBus.Instance.Raise(new StatusEffectRemovedEvent
                {
                    TargetInstanceId = entity.RuntimeData.RuntimeInstanceId,
                    EffectId = removedId
                });
            }
        }
    }
}
