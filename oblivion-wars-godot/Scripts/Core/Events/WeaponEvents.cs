public struct WeaponSwitchedEvent : IGameEvent
{
    public string NewWeaponId;
    public string PreviousWeaponId;
}

public struct ForceWeaponSelectEvent : IGameEvent
{
    public string WeaponId;
}
