using System.Collections.Generic;

// Derived from the definition files, this is the runtime data that is referenced.
// Set as part of initialization of an entity.
// This data could be wiped each save.
public class EntityRuntimeData
{
    public string EntityId;
    public ulong RuntimeInstanceId;

    public float CurrentHealth;
    public float MaxHealth;
    public bool IsInvincible;
}
