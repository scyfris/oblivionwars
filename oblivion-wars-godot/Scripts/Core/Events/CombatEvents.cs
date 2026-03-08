using Godot;

public interface IGameEvent { }

public struct HitEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public ulong SourceInstanceId;
    public float BaseDamage;
    public float ImpactForce;
    public Vector2 HitDirection;
    public Vector2 HitPosition;
}

public struct EntityDiedEvent : IGameEvent
{
    public ulong EntityInstanceId;
    public ulong KillerInstanceId;
    public Vector2 Position;
}

public struct DamageAppliedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public float FinalDamage;
}
