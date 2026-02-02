public struct StatusEffectAppliedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public string EffectId;
}

public struct StatusEffectRemovedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public string EffectId;
}
