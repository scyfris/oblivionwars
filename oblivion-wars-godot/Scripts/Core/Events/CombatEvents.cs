using Godot;

public interface IGameEvent { }

public struct HitEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public ulong SourceInstanceId;
    public float BaseDamage;
    public Vector2 HitDirection;
    public Vector2 HitPosition;
}

public struct EntityDiedEvent : IGameEvent
{
    public ulong EntityInstanceId;
    public ulong KillerInstanceId;
}

public struct DamageAppliedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public float FinalDamage;
}
