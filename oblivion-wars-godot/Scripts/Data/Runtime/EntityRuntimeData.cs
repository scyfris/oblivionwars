using System.Collections.Generic;

public class EntityRuntimeData
{
    public string EntityId;
    public ulong RuntimeInstanceId;

    public float CurrentHealth;
    public float MaxHealth;

    public List<ActiveStatusEffect> StatusEffects = new();

    public CharacterDefinition Definition;
}

public class ActiveStatusEffect
{
    public string EffectId;
    public float RemainingDuration;
    public float TickTimer;
    public int CurrentStacks;
    public StatusEffectDefinition Definition;
}
